using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICSharpCode.SharpZipLib.Tar;

using Microsoft.Extensions.Logging;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Generation.Edi835.Application;
using VPay.DocSys.Generation.Edi835.Application.PayFile;
using VPay.DocSys.Storage;
using DS=VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835
{
    public class Edi835CreationService : IEdi835CreationService
    {
        private readonly Edi835ServiceSettings _settings;
        private readonly IDocsParserFactory _docsParserFactory;
        private readonly ILogger<Edi835CreationService> _logger;
        private readonly IPayfileService _payfileService;
        private readonly ITrnService _trnService;
        private readonly IStorageManager _storageManager;
        private readonly IFileSystem _fileSystem;
        private readonly MatchingCriteria _pdfMatchingCriteria;

        public Edi835CreationService(
            Edi835ServiceSettings settings,
            IDocsParserFactory docsParserFactory,
            ILogger<Edi835CreationService> logger,
            IPayfileService payfileService,
            ITrnService trnService,
            IStorageManager storageManager,
            IFileSystem fileSystem
            )
        {
            _settings = settings;
            _docsParserFactory = docsParserFactory;
            _logger = logger;
            _payfileService = payfileService;
            _trnService = trnService;
            _storageManager = storageManager;
            _fileSystem = fileSystem;
            _pdfMatchingCriteria = new MatchingCriteria(_settings.PdfFileNamesEndWith);
        }

        #region GeneratePayfileFile


        public async Task<Edi835File> GeneratePayfileFile(DS.Edi835 message)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));
            var payfileContent = await _payfileService.GeneratePayFileDocument(message, default).ConfigureAwait(false);
            return new PayfileFile
            {
                Content = payfileContent,
                Filename = message.GetFilename("payfile")
            };
        }

        #endregion

        #region GenerateDocsFile

        public async Task<Edi835File> GenerateDocsFile(DS.Edi835 message)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));

            try
            {
                var parser = await _docsParserFactory.GetDocsParser(message.ClientCode);
                var docsContent = await parser.LocateContent(message, default).ConfigureAwait(false);
                return docsContent;
            }
            catch (FileNotFoundException fnfEx)
            {
                using (_logger.BeginScope(message.ToDictionary()))
                {
                    _logger.LogError(fnfEx, ".docs file not Found");
                }
            }
            catch (Exception)
            {
                throw;
            }

            // An empty .docs file content will give the caller the "not found" path.  
            return new DocsFile();
        }

        #endregion

        #region GenerateTrnFile

        public async Task<Edi835File> GenerateTrnFile(DS.Edi835 message)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));

            var trnContent = await _trnService.GenerateTrnDocument(message.NonFundedPaymentDetailId, default);

            return new TrnFile
            {
                Content = trnContent,
                Filename = message.GetFilename("trn")
            };
        }

        #endregion

        #region GetPdfFile


        /// <summary>
        /// Tries to find the appropriate pdf to attach to the 835 package. There is a LOT
        /// of detail in the exceptions thrown to make determining why the error is happening
        /// easier to diagnose for production support, in the case any errors do occur.
        /// </summary>
        /// <returns>A PdfFile with the appropriate values.</returns>
        public async Task<IArchiveDetailStream> FetchPdfFor(DS.Edi835 message)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));

            var isProd = (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "").ToUpper().StartsWith("PROD");
            //    || message.FetchPdfInNonProd; // This option will allow this code to be tested in Dev/Stage
            //// However, the zip archive (created from Parsing) will need to exist for the transaction in that
            //// environment.

            if (!isProd)
            {
                // This is the exact behavior of Legacy (NON-Prod always returns this).
                return new FakePdfArchiveDetailStream(message);
            }

            IEnumerable<IArchiveFile> archives = null;

            try
            {
                archives = await _storageManager.GetArchivesForTransactionId(message.TransactionId);
            }
            catch (Exception storageManagerException)
            {
                // If storage manager has any kind of connection problems, we can't do anything else at
                // this point, bail.
                throw new PdfFileNotFoundException(
                    "Edi835 Pdf Fetch Failed: StorageMananger threw exception.  See InnerException for details",
                    message,
                    storageManagerException);
            }

            // It's possible there is more than one archive (fax). However, sometimes multiples get created, and
            // that's another issue, but identifying would be helpful to whoever has to fix the issue.
            var archiveFiles = archives.Where(a =>
                a.DocumentSystemType == DocumentSystemType.Generic && // DocumentSystemType.Transaction is a FAX archive...
                a.TransactionId == message.TransactionId
            ).ToList();

            // There should be 1 and only 1 matching archive...
            if (archiveFiles.Count != 1)
            {
                if (archiveFiles.Count == 0)
                {
                    throw new PdfFileNotFoundException("Edi835 Pdf Fetch Failed: Unable to find ArchiveFile for transaction.", message);
                }
                if (archiveFiles.Count > 1)
                {
                    throw new PdfFileNotFoundException("Edi835 Pdf Fetch Failed: Found multiple ArchiveFiles for transaction.", message);
                }
            }

            var archiveFile = archiveFiles.Single();
            var archiveDetail = archiveFile.ArchiveDetails.FirstOrDefault(d => _pdfMatchingCriteria.Matches(d.FileName));

            if (archiveDetail == null)
            {
                // If an ArchiveDetail matching the criteria isn't found.
                string errorMessage = $"Edi835 Pdf Fetch Failed: Unable to find ArchiveDetail for transaction in ArchiveFileId: {archiveFile.ArchiveFileId}.";
                throw new PdfFileNotFoundException(errorMessage, message);
            }

            string zipInputFileName = _fileSystem.Path.Combine(archiveFile?.PathName ?? "", archiveFile?.FileName ?? "");

            return new ArchiveDetailStream(zipInputFileName, archiveDetail);
        }




        /// <summary>
        /// Tries to find the appropriate pdf to attach to the 835 package. There is a LOT
        /// of detail in the exceptions thrown to make determining why the error is happening
        /// easier to diagnose for production support, in the case any errors do occur.
        /// </summary>
        /// <returns>A PdfFile with the appropriate values.</returns>
        public async Task<Edi835File> GetPdfFile(DS.Edi835 message)
        {
            var archiveDetail = await FetchPdfFor(message);

            PdfFile pdfFile = new PdfFile();
            pdfFile.ReadArchiveDetailStream(archiveDetail);
            pdfFile.Filename = message.GetFilename("pdf");

            return pdfFile;
        }
        #endregion

        #region GenerateTarPackage/GeneratePackage

        public async Task<TarPackage> GenerateTarPackage(DS.Edi835 message)
        {
            var results = await GeneratePackage(message);

            TarPackage tarPackage = new TarPackage
            {
                Filename = message.GetFilename("tar"),
                Stream = new MemoryStream()
            };

            var tarOutputStream = new TarOutputStream(tarPackage.Stream, Encoding.UTF8)
            {
                IsStreamOwner = false
            };

            foreach (var r in results)
            {
                AddToTar(tarOutputStream, r);
            }

            tarOutputStream.Finish();
            tarOutputStream.Close();
            // Reset the stream to the beginning so the caller can read it immediately.
            tarPackage.Stream.Position = 0;

            return tarPackage;
        }



        public async Task<IEnumerable<Edi835File>> GeneratePackage(DS.Edi835 message)
        {
            return await Task.WhenAll(
                 GenerateDocsFile(message),
                 GeneratePayfileFile(message),
                 GenerateTrnFile(message),
                 GetPdfFile(message));
        }




        private void AddToTar(TarOutputStream tarStream, Edi835File ediFile)
        {
            using (var contentStream = ediFile.ContentAsStream())
            {
                if (contentStream.Length == 0)
                {
                    _logger.LogDebug("ContentStream of {Filename} is empty.", ediFile.Filename);
                    return;
                }

                _logger.LogDebug("Content.Length for {Filename} is {ContentLength}.", ediFile.Filename, contentStream.Length);

                TarEntry entry = TarEntry.CreateTarEntry(ediFile.Filename);
                entry.Size = contentStream.Length;

                tarStream.PutNextEntry(entry);
                contentStream.CopyTo(tarStream);
            }

            tarStream.CloseEntry();
        }


        #endregion


    }
}
