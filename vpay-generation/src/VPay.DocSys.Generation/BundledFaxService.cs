using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;
using VPay.Data.Db2.Abstractions;
using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Models.Sql;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Data;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation
{
    public class BundledFaxService : IBundledFaxService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IStorageManager _storageManager;
        private readonly IPdfTools _pdfTools;
        private readonly ILogger<BundledFaxService> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IFaxManager _faxManager;
        private readonly DocSysSettings _configuration;
        private readonly IFopApiClient _fopApiClient;
        private readonly IFax _faxRepository;
        private readonly ITiffMessageQueuer _tiffMessageQueuer;
        private readonly IBundledFaxRepository _bundledFaxRepository;

        public BundledFaxService(
            IFileSystem fileSystem,
            IStorageManager storageManager,
            IFaxManager faxManager,
            IPdfTools pdfTools,
            ILoggerFactory loggerFactory,
            IFopApiClient fopApiClient,
            DocSysSettings configuration,
            IFax faxRepository,
            ITiffMessageQueuer tiffMessageQueuer,
            IBundledFaxRepository bundledFaxRepository
            )
        {
            ArgumentHelpers.ThrowIf.IsNull(loggerFactory, nameof(loggerFactory));
            ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ArgumentHelpers.ThrowIf.IsNull(storageManager, nameof(storageManager));
            ArgumentHelpers.ThrowIf.IsNull(faxManager, nameof(faxManager));
            ArgumentHelpers.ThrowIf.IsNull(pdfTools, nameof(pdfTools));
            ArgumentHelpers.ThrowIf.IsNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIf.IsNull(fopApiClient, nameof(fopApiClient));
            ArgumentHelpers.ThrowIf.IsNull(faxRepository, nameof(faxRepository));
            ArgumentHelpers.ThrowIf.IsNull(tiffMessageQueuer, nameof(tiffMessageQueuer));
            ArgumentHelpers.ThrowIf.IsNull(bundledFaxRepository, nameof(bundledFaxRepository));

            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<BundledFaxService>();
            _fileSystem = fileSystem;
            _storageManager = storageManager;
            _faxManager = faxManager;
            _pdfTools = pdfTools;
            _configuration = configuration;
            _fopApiClient = fopApiClient;
            _faxRepository = faxRepository;
            _tiffMessageQueuer = tiffMessageQueuer;
            _bundledFaxRepository = bundledFaxRepository;
        }

        private static void SetDefaults(BundledFaxParams input)
        {
            if (string.IsNullOrEmpty(input.Subject))
            {
                input.Subject = BundledFax.DefaultSubject;
            }
            if (string.IsNullOrEmpty(input.FromFaxNumber))
            {
                input.FromFaxNumber = BundledFax.DefaultFromFax;
            }

            input.Date = DateTime.Now;
        }

        public async Task<bool> CreateBundledFax(BundledFaxParams input)
        {
            bool success = false;
            long bundledFaxId = 0;

            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                {"ToFaxNumber", input.ToFaxNumber},
                {"UserName", input.UserName}
            }))
            {
                try
                {
                    SetDefaults(input);

                    bundledFaxId = await _faxManager.InsertBundledFax(input);
                    _logger.LogInformation($"Starting Fax Creation");

                    Guid archiveNameGuid = Guid.NewGuid();
                    _logger.LogInformation($"Bundled Fax PDF guid={archiveNameGuid}");

                    IArchiveFile archiveFile = new ArchiveFile(
                        input.UserName.SafeSubstring(0, 12),
                        string.Empty,
                        0,
                        Guid.Empty,
                        string.Empty)
                    {
                        CreateLogin = input.UserName.SafeSubstring(0, 100)
                    };

                    _logger.LogInformation($"Bundled Fax result={_configuration.ResultPath}");
                    string tempFolder = _fileSystem.Path.Combine(_configuration.ResultPath, archiveNameGuid.ToString());
                    _logger.LogInformation($"Bundled Fax temp={tempFolder}");
                    _fileSystem.Directory.CreateDirectory(tempFolder);

                    List<IGeneratedFile> details = new List<IGeneratedFile>();
                    var pdfList = new List<string>();

                    if (input.DetailIds != null)
                    {
                        _logger.LogInformation("Getting streams");
                        var tasks = input.DetailIds?.Select(d =>
                            _storageManager.GetArchiveDetailStream(d));
                        var getStreams = Task.WhenAll(tasks);
                        var results = await getStreams;

                        if (results.Any(r => r == null))
                        {
                            var foundDetails = results.Where(r => r != null).Select(r => r.ArchiveDetailId).ToList();
                            var missing = input.DetailIds.Except(foundDetails).ToList();
                            throw new ApplicationException("Unable to download all requested details " +
                                                           string.Join(",", missing));
                        }

                        foreach (var result in results)
                        {
                            string tempFileLocation = _fileSystem.Path.Combine(tempFolder, result.FileName);

                            //If they have selected multiple details with the same file name
                            //we don't want to overwrite files we have already downloaded
                            if (_fileSystem.File.Exists(tempFileLocation))
                            {
                                string newFile = _fileSystem.Path.GetFileNameWithoutExtension(tempFileLocation);
                                int fileCounter = 1;
                                //handle an arbitrary number of files with same name by adding a numbered suffix
                                while (true)
                                {
                                    if (!_fileSystem.File.Exists(tempFileLocation.Replace(".pdf", $"_{fileCounter}.pdf")))
                                    {
                                        newFile += $"_{fileCounter}.pdf";
                                        break;
                                    }

                                    fileCounter++;
                                }

                                tempFileLocation = _fileSystem.Path.Combine(tempFolder, newFile);
                            }

                            //Create hidden detail records to establish the relationship between the transaction IDs of each detail
                            //with the merged fax document
                            details.Add(new EobGeneratedFile()
                            {
                                FileName = tempFileLocation,
                                Description = result.Description,
                                FileDetailType = FileDetailType.Hidden,
                                PageCount = result.PageCount,
                                TransactionId = result.TransactionId.ToString()
                            });

                            // write archive detail to temp file
                            using (var writeStream = _fileSystem.File.Create(tempFileLocation))
                            {
                                using (var readStream = result.Open())
                                {
                                    readStream.CopyTo(writeStream);
                                    readStream.Close();
                                }

                                writeStream.Close();
                            }
                        }

                        pdfList = details.Select(d => d.FileName).ToList();
                    }
                    else
                    {
                        throw new ApplicationException("No details. Nothing to bundle.");
                    }

                    var detailsPDF = _fileSystem.Path.Combine(tempFolder, archiveNameGuid + "-temp.pdf");
                    var bundlePDF = _fileSystem.Path.Combine(tempFolder, archiveNameGuid + ".pdf");

                    _ = await _pdfTools.MergeAsync(detailsPDF, pdfList);

                    //Set the page count to the number of pages of the combined details plus one page for the cover sheet
                    input.PageCount = await _pdfTools.GetNumberOfPagesAsync(detailsPDF) + 1;

                    //Clean up individual detail files now that they are merged
                    foreach (var sourceFile in details)
                    {
                        _fileSystem.File.Delete(sourceFile.FileName);
                    }

                    var coverLocation = await CreateCoverPage(input);
                    _ = await _pdfTools.MergeAsync(bundlePDF, new List<string>() { coverLocation, detailsPDF });

                    details.Add(new EobGeneratedFile()
                    {
                        FileName = bundlePDF,
                        Description = "Bundled Fax",
                        FileDetailType = FileDetailType.BundledFax,
                        PageCount = input.PageCount
                    });

                    var archiveFileId = await _storageManager.CreateArchive(archiveFile, details, true);
                    archiveFile = await _storageManager.GetArchiveForArchiveFileId(archiveFileId);

                    _ = await _faxManager.InsertBundledFaxStatus(bundledFaxId, FaxManager.FaxStatus.ArchiveCreated, null);

                    var tempTransactionList = details
                        .Where(d => d.TransactionId != null)
                        .Select(d => long.Parse(d.TransactionId))
                        .Distinct()
                        .OrderBy(_ => _)
                        .ToList();

                    var bundledFaxCorrespondenceIdResult = await GenerateBundledFaxCorrespondenceId(
                        tempTransactionList,
                        input.ToFaxNumber,
                        _fileSystem.Path.GetFileName(bundlePDF),
                        input.PageCount);

                    using (_logger.BeginScope(new Dictionary<string, object>
                    {
                        { "CorrespondenceId", bundledFaxCorrespondenceIdResult.CorrespondenceId }
                    }))
                    {
                        var message = FormulateBundledFaxMessage(
                            bundledFaxCorrespondenceIdResult,
                            input,
                            tempTransactionList,
                            archiveFile);

                        _tiffMessageQueuer.QueueTiffMessage(message);

                        _ = await _faxManager.InsertBundledFaxStatus(bundledFaxId, FaxManager.FaxStatus.QueuedForTransmit, null);
                    }

                    success = true;
                }
                catch (Exception ex)
                {
                    success = false;
                    _logger.LogError(ex, "Failed to create bundled Fax.");
                    _ = await _faxManager.InsertBundledFaxStatus(bundledFaxId, FaxManager.FaxStatus.Failed, null);
                }
            }

            return success;
        }

        public TiffQueueMessage FormulateBundledFaxMessage(
            BundledFaxCorrespondenceIdResult bFaxResult,
            BundledFaxParams bFaxParams,
            List<long> transactions,
            IArchiveFile archiveFile)
        {
            var bundledFaxDetail = archiveFile
                .ArchiveDetails
                .SingleOrDefault(_ => _.FileDetailType == FileDetailType.BundledFax);

            if (bundledFaxDetail == null)
            {
                _logger.LogError("No {FileDetailType} detail found in {ArchiveFileId}", FileDetailType.BundledFax, archiveFile.ArchiveFileId);

                throw new Exception($"Archive File Id: {archiveFile.ArchiveFileId} has no {FileDetailType.BundledFax} File Type");
            }

            return new TiffQueueMessage()
            {
                TransactionIds = transactions,
                CorrespondenceId = bFaxResult.CorrespondenceId,
                ArchiveFileId = archiveFile.ArchiveFileId,
                ArchiveDetailId = bundledFaxDetail.ArchiveDetailId,
                PayType = bFaxResult.PaymentTypeCode,
                Priority = 15,
                FaxNumber = bFaxParams.ToFaxNumber,
                BillingEntity = bFaxResult.BillingEntityCode,
                Tpa = bFaxResult.ClientCode,
                Origin = bFaxResult.OriginCode,
                PrintVendor = "FSSI",
                BoardUploadTimeStamp = DateTime.Now,
                ReserveName = FaxReserveName.BFAX,
                ErrorRetryCount = 0
            };
        }

        public async Task<string> CreateCoverPage(BundledFaxParams input)
        {
            XslCompiledTransform myXslTransform = new XslCompiledTransform();
            var document = new XmlDocument();
            var navigator = document.CreateNavigator();

            StringBuilder foData = new StringBuilder();
            using(var xwriter = XmlWriter.Create(navigator.AppendChild()))
            {
                var serializer = new XmlSerializer(typeof(BundledFaxParams));
                serializer.Serialize(xwriter, input);
            }

            XmlWriter foWriter = XmlWriter.Create(foData, new XmlWriterSettings()
            {
                OmitXmlDeclaration = true
            });

            myXslTransform.Load(XmlReader.Create(new StringReader(BundledFax.Stylesheet)));
            myXslTransform.Transform(document, foWriter);

            var coverPageStream = (await _fopApiClient.PostFile(foData.ToString())) as MemoryStream;

            string filename = _fileSystem.Path.GetTempFileName();
            _fileSystem.File.WriteAllBytes(filename, coverPageStream.ToArray());

            return filename;
        }

        public async Task<List<string>> RetryFailedBundledFaxesAsync(DateTime startDate, long? bundledFaxId)
        {
            List<string> resultMessageList = new List<string>();
            string resultMessage = "";
            var failedBundledFaxList = await GetFailedBundledFaxesAsync(startDate);
            if(bundledFaxId != null)
            {
                failedBundledFaxList = failedBundledFaxList.Where(d => d.Id == bundledFaxId).ToList();
            }

            if (failedBundledFaxList == null || failedBundledFaxList.Count() == 0)
            {
                resultMessageList.Add("no failed bundled faxes were found");
                _logger.LogInformation($"{resultMessageList.First()} in RetryFailedBundledFaxesAsync for {startDate}");
                return resultMessageList;
            }
            foreach (var bundledFax in failedBundledFaxList)
            {

                bool tempResult = true;
                BundledFaxParams tempParams = new BundledFaxParams();
                tempParams = JsonConvert.DeserializeObject<BundledFaxParams>(JsonConvert.SerializeObject(bundledFax));

                try
                {
                    tempResult = await CreateBundledFax(tempParams);
                }
                catch
                { tempResult = false; }

                if (!tempResult)
                { resultMessage = $"{bundledFax.Id} failed again  "; }

                else
                { resultMessage = $"{bundledFax.Id} was successfully re-processed  "; }

                using (_logger.BeginScope(new Dictionary<string, object>()
                {
                    {"BundledFaxId", bundledFax.Id}
                }))
                {
                    _logger.LogInformation($"{resultMessage} in RetryFailedBundledFaxesAsync for {startDate}");
                }
                resultMessageList.Add(resultMessage);
            }
            return resultMessageList;
        }

        public Task<List<FailedBundledFax>> GetFailedBundledFaxesAsync(DateTime startDate)
        {
            return _bundledFaxRepository.GetFailedBundledFaxesAsync(startDate.Date);
        }

        public Task<BundledFaxCorrespondenceIdResult> GenerateBundledFaxCorrespondenceId(List<long> transactionIds, string faxNumber, string documentUrl, int pageCount)
        {
            var transactionObjects = transactionIds
             .Select(d => new BundledFaxCorrespondenceIdPayload.TransactionObject { TransactionId = d })
             .ToList();

            BundledFaxCorrespondenceIdPayload payload = new BundledFaxCorrespondenceIdPayload()
            {
                TransactionIds = transactionObjects,
                FaxNumber = faxNumber,
                PdfLocation = documentUrl,
                PageCount = pageCount
            };

            return _faxRepository.GenerateBundledFaxCorrespondenceId(payload);
        }
    }
}
