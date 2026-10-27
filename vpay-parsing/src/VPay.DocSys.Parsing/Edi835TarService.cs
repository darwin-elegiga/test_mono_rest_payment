using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Extensions;

namespace VPay.DocSys.Parsing
{
    public class Edi835TarService : IEdi835TarService
    {
        private readonly IFileSystem _fileSystem;
        private readonly Edi835ServiceSettings _edi835ServiceSettings;
        private readonly Edi835ConfigurationCache _edi835ConfigurationCache;
        private readonly IDocsExtractorFactory _docsExtractorFactory;
        private readonly IEdi835Repository _edi835Repository;
        private readonly ILogger<Edi835TarService> _logger;
        private readonly string _tarHome;
        private readonly string _savePathRoot;
        private readonly bool _enableEdi835Parsing;

        public Edi835TarService(
            Edi835ServiceSettings edi835ServiceSettings,
            Edi835ConfigurationCache edi835ConfigurationCache,
            IDocsExtractorFactory docsExtractorFactory,
            IEdi835Repository edi835Repository,
            IFileSystem fileSystem,
            ILogger<Edi835TarService> logger)
        {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _edi835ServiceSettings = edi835ServiceSettings ?? throw new ArgumentNullException(nameof(edi835ServiceSettings));
            _edi835ConfigurationCache = edi835ConfigurationCache ?? throw new ArgumentNullException(nameof(edi835ConfigurationCache));
            _edi835Repository = edi835Repository;
            _docsExtractorFactory = docsExtractorFactory;
            _tarHome = _fileSystem.Path.Combine(_edi835ServiceSettings.FileHome, _edi835ServiceSettings.TarDataFolder);
            _enableEdi835Parsing = _edi835ServiceSettings.EnableEdi835Parsing;
            _savePathRoot = _fileSystem.Path.Combine(_edi835ServiceSettings.FileHome, _edi835ServiceSettings.Edi835RootFolder, _edi835ServiceSettings.DocsFolder);

            // Summary:
            //     Provides access to an encoding provider for code pages that otherwise are available
            //     only in the desktop .NET Framework.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Extract the .docs files from the client input file.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IList<Edi835>> ExtractDocs(List<DocumentTransactionMessage> messages, CancellationToken cancellationToken)
        {
            List<Edi835> returnList = new List<Edi835>();

            if (!_enableEdi835Parsing)
            {
                _logger.LogInformation("Edi835 parsing is disabled.");
                return returnList;
            }

            var firstMessage = messages.FirstOrDefault();
            if (firstMessage == null)
            {
                _logger.LogInformation("Must have at least 1 message to proceed.");
                return returnList; 
            }

            var stsFilename = Path.GetFileName(firstMessage.StatusFilePath);
            if (stsFilename.IsReIssueFile() || stsFilename.IsDropToPrintFile())
            {
                _logger.LogInformation("Reissue/D2P files do not get 835s.");
                return returnList;
            }
            var onlyEdi835messages = messages.Where(m => m.IsEdi835Needed(_edi835ConfigurationCache)).ToList();
            if (!onlyEdi835messages.Any())
            {
                _logger.LogInformation("The message list does not contain any payments requiring an 835 be generated.");
                return returnList;
            }

            var clientCode = firstMessage.TPA.Trim().ToUpper();
            var origin = firstMessage.StatusRecord?.ORIGIN?.Trim().ToUpper() ?? string.Empty;
            string inputEntryName = $"{firstMessage.InputFile}_processed/{firstMessage.InputFile}";
            string tarDataFilename = Path.Combine(_tarHome, $"{firstMessage.InputFile}_processed.tar");            

            // RC, no docs file
            if (!string.IsNullOrWhiteSpace(origin) && origin.Equals("RC"))
            {
                var transactionIds = onlyEdi835messages.Select(_ => Convert.ToInt64(_.TransactionId.RemoveLeadingZeroes() ?? "0"));
                return await _edi835Repository.Create835s(
                    transactionIds,
                    GetType().FullName,
                    "ExtractDocs",
                    cancellationToken
                );
            }
            else
            {
                var docsExtractor = await _docsExtractorFactory.GetDocsExtractor(clientCode);
                foreach (var doc in docsExtractor.ExtractDocs(tarDataFilename, inputEntryName, onlyEdi835messages))
                {
                    returnList.Add(await SaveFileAndCreate835(doc, cancellationToken));
                }
            }
            return returnList;
        }

        public bool EnableEdi835Parsing
        {
            get
            {
                return _edi835ServiceSettings.EnableEdi835Parsing;
            }
        }

        internal StatusFile GetStatusFile(string stsFilename)
        {
            var statusFile = new StatusFile(_fileSystem.File.ReadLines(stsFilename).ToArray());
            statusFile.Records.RemoveAll(r => r.DIST == DistType.Unload);
            statusFile.Header.Count = statusFile.Records.Count;
            return statusFile;
        }
        public List<DocumentTransactionMessage> ParseSts(string stsFilename)
        {

            var statusFile = GetStatusFile(stsFilename);
            var isRedCard = statusFile.Header.Destination.Trim().Equals("RC", StringComparison.InvariantCultureIgnoreCase);
            var messages =
              (from record in statusFile.Records
               where record.DESC != "DENIED"
               select new DocumentTransactionMessage
               {
                   //Re-issue uses EOB from the original transaction
                   GenerateEOB = false,

                   StatusFilePath = stsFilename,
                   RegeneratePackage = false,
                   InputFile = statusFile.Header.InputFile,
                   TransactionId = record.TXID,
                   TPA = record.TPA,
                   IsRedCard = isRedCard,
                   GUID = record.GUID,
                   BillingEntity = record.BILENT,
                   CDate = statusFile.Header.CDate,
                   StatusRecord = record,
                   ConfigData = ""
               }).ToList();
            return messages;

        }

        /// <summary>
        /// Saves the parsed .docs file to the file system
        /// </summary>
        /// <remarks>It will not fail if the file/path already exists, it will overwrite in that case</remarks>
        /// <param name="docsContent"></param>
        private void SaveFile(Edi835ParsedDocsContent docsContent)
        {
            if (docsContent?.DocumentTransactionMessage == null || string.IsNullOrWhiteSpace(docsContent.Content)) { return; }

            var m = docsContent.DocumentTransactionMessage;
            var saveFolder = Path.Combine(_savePathRoot, m.TPA, m.BillingEntity, m.InputFile);
            // will not fail if the named dir already exists.
            _fileSystem.Directory.CreateDirectory(saveFolder);

            var docsFilename = Path.Combine(saveFolder, docsContent.TempFilename + ".docs");
            if (_fileSystem.File.Exists(docsFilename))
            {
                _fileSystem.File.Delete(docsFilename);
            }
            _fileSystem.File.WriteAllText(docsFilename, docsContent.Content, new UTF8Encoding(false));
        }

        /// <summary>
        /// Saves the .docs file to the disk and inserts an Edi835 record into the database
        /// </summary>
        /// <param name="docsContent"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Edi835> SaveFileAndCreate835(Edi835ParsedDocsContent docsContent, CancellationToken cancellationToken)
        {
            SaveFile(docsContent);
            return await _edi835Repository.Create835(
                docsContent.TransactionId,
                GetType().FullName,
                "ExtractDocs",
                cancellationToken);
        }
    }
}
