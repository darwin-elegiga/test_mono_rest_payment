using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Edi835;

using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835
{
    public class Edi835GenerationMessageProcessor : IEdi835GenerationMessageProcessor
    {
        private readonly Edi835ServiceSettings _settings;
        private readonly IFileSystem _fileSystem;
        private readonly IEdi835CreationService _edi835CreationService;
        private readonly IEdi835Repository _edi835Repository;
        private readonly IFileTransferService _fileTransferService;
        private readonly ILogger<Edi835GenerationMessageProcessor> _logger;

        public Edi835GenerationMessageProcessor(
            Edi835ServiceSettings settings,
            IFileSystem fileSystem,
            IEdi835CreationService edi835CreationService,
            IEdi835Repository edi835Repository,
            IFileTransferService fileTransferService,
            ILogger<Edi835GenerationMessageProcessor> logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _edi835CreationService = edi835CreationService ?? throw new ArgumentNullException(nameof(edi835CreationService));
            _edi835Repository = edi835Repository ?? throw new ArgumentNullException(nameof(edi835Repository));
            _fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessMessage(string message, CancellationToken cancellationToken)
        {
            /* Message comes in... It's either in generated or not... check that first... */
            var edi835 = UnwrapMessage(message);

            var currentEdi835 = await _edi835Repository.Get835(edi835.Edi835Id, cancellationToken);
            if (currentEdi835 == null)
            {
                var msg = $"Cannot find Edi835Id {edi835.Edi835Id} for TransactionId {edi835.TransactionId}.";
                /*
                 If the endpoint was called for the Optum banking information, and the sts has not
                yet been parsed, (or cleandocs called) then the Edi835 record would not exist. So, it never would have queued
                it in the first place. Any other scenario would be caught here...
                 */
                throw new ApplicationException(msg); // Put the message in the error queue
            }
            else if (!currentEdi835.CanBeQueuedForGeneration())
            {
                /*
                 The currentEdi835 could have been canceled by the user...
                    OR no "active" Edi835 record inserted...
                    OR, it's already been processed...
                all of these are scenarios where this message shouldn't cause an error.
                 */
                var msg = currentEdi835.IsTerminalStatus ? " is terminal " : " has already been processed ";
                _logger.LogWarning("Edi835Id {Edi835Id} " + msg + " for {TransactionId}. [{Edi835StatusCode}]",
                    currentEdi835.Edi835Id,
                    currentEdi835.TransactionId,
                    currentEdi835.StatusCode
                    );
                return; // this will assume the message was processed in a way that allows it to be ACK'ed by Rabbit.
                // Using explicit return here more for the reader than the compiler...
            }
            else
            {
                bool resetStatusOnException = false;
                try
                {
                    /* At this point, nothing has been changed in the system of record about this rabbit message. */
                    await _edi835Repository.Add835Status(
                        currentEdi835.Edi835Id,
                        Edi835StatusCode.Generating,
                        "Edi835 Generation starting", "",
                        "Edi835GenerationConsumer",
                        "System",
                        cancellationToken
                        );
                    /* Db thinks this message is generating, but it is not yet "Generated" AND there's nothing written to .tar AND rabbit is currently in limbo.
                     If we do throw an error, we definitely want to reset this to allow it to be shoveled from the error queue */
                    resetStatusOnException = true;
                    _logger.LogInformation("Starting Edi835 Package Generation for {TransactionId}", currentEdi835.TransactionId);
                    /* if the following throws any errors, the DB _should_ reset in any case
                     *
                     Scenarios that I can imagine for things that could go wrong:
                        PdfFileNotFoundException:
                            if this is an OPACH payment, it's possible this was called prior to the pdf being available.
                                That's OK.  We want to ACK the message and set this back to Edi835StatusCode.QueuedForGeneration
                                    It will be called later, when the Banking information is updated for this payment.
                        OpachBankingDetailsMissingException
                            Distribution was complete and this is an OPACH payment.  but the banking information was missing...
                                That's OK.  We want to ACK the message and set this back to Edi835StatusCode.QueuedForGeneration
                                    It will be called later, when the Pdf becomes available and distribution is called.
                    OTHER scenarios that could cause errors (Would mostly throw ApplicationException):
                        NFS mount failure (would NOT throw ApplicationException... still not entirely sure how to deal with this one...)
                        .docs file not found / generated:
                            NOT (ClientCode in ('SWK','HEQTN')  OR OriginCode='RC') -- since the only currently implemented options are RC/SWK/HEQTN, this is a bigger problem...
                            _processed.tar not found.  (SWK/HEQTN) (we don't have logic that looks in tarData/archive/yyyy/MM...
                            .docs file not found in /SRVFS/bogner/transfer/835/docs (RC)
                                -- Legacy, if it processed this, takes the file from /SRVFS/bogner/transfer/835/docs/{userKey}_DD_000000000.docs and moves to
                                -- ../archive/docs/yyyy/MM/dd/(Then a fully different filename with a specific timestamp from the legacy system)
                                -- these scenarios only apply during migration...
                                -( AND not found in either legacy db  OR the .tgz) <--- if we can't find RC .docs file in archive on filesystem
                                   -- we look in the .tgz that we sent to SDS...
                                - mysql > VPAYDW replication failed / out of sync

                        .trn not found (DB2 can't connect/time out, NonFundedPaymentDetailId not found)
                        .payfile failed for a NON-Optum bank info reason
                            - can't reach http(s) endpoint
                            - the key would not work ApiToken missing/invalid/rejected
                            - connection timeout/refused/5xx error code
                            - bank info not found

                         Out of memory (one of .docs/.trn/.payfile/.pdf too big, OR the in-memory .tar TOO big)

                     */
                    var tarPackage = await _edi835CreationService.GenerateTarPackage(currentEdi835);
                    /*
                     if the follow throws any errors, the DB _should_ reset in any case
                        NFS mount problems
                        directory no found
                        unauthorized
                        file exists
                     */
                    var tarFilename = WriteTarToFolder(tarPackage);

                    _logger.LogInformation("Finished Edi835 Package Generation for {TransactionId}", currentEdi835.TransactionId);
                    await _edi835Repository.Add835Status(
                        currentEdi835.Edi835Id,
                        Edi835StatusCode.GenerationComplete,
                        "Edi835 Generation complete", tarFilename,
                        "Edi835GenerationConsumer",
                        "System",
                        cancellationToken
                        );
                    /*
                     At this point, we have a .tar with all the stuff in it,
                    and we marked this as generate, regardless of what happens at this point... we don't want to "re-generate" this
                    without some manual intervention...
                     */
                    resetStatusOnException = false;
                    if (_settings.TarFileTransfer.Enabled)
                    {
                        _logger.LogInformation("Starting transfer of Edi835 package for {TransactionId}", currentEdi835.TransactionId);
                        await TransferTarFile(tarFilename, cancellationToken);
                        _logger.LogInformation("Finished transferring Edi835 Package for {TransactionId}", currentEdi835.TransactionId);

                        await _edi835Repository.Add835Status(
                            currentEdi835.Edi835Id,
                            Edi835StatusCode.SentToVendor,
                            $"835 tar file transferred to {_settings.TarFileTransfer.Host} for distribution to vendor.",
                            tarFilename,
                            "Edi835GenerationConsumer",
                            "System",
                            cancellationToken).ConfigureAwait(false);

                        await ArchiveRedCardDocsFile(currentEdi835).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.LogInformation("Tar file transfer disabled - skipping tar file transfer for {TransactionId}", currentEdi835.TransactionId);

                        await _edi835Repository.Add835Status(
                            currentEdi835.Edi835Id,
                            Edi835StatusCode.SentToVendor,
                            $"Tar file transfer feature disabled, no file was transferred for distribution to vendor.",
                            tarFilename,
                            "Edi835GenerationConsumer",
                            "System",
                            cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    if (IsSkipOpach(ex))
                    {
                        _logger.LogInformation("Edi835 Package Generation could not generate because OPACH bank details have not been setup.");
                    }
                    else
                    {
                        _logger.LogError(ex, "Edi835 Package Generation failed, see exception for details.");
                    }
                    if (resetStatusOnException)
                    {
                        await _edi835Repository.Add835Status(
                             edi835.Edi835Id,
                             Edi835StatusCode.QueuedForGeneration,
                             "Queued For Generation - reset due to exception", "",
                             "Edi835GenerationConsumer",
                             "System",
                             cancellationToken
                            ).ConfigureAwait(false);
                    }
                    throw; // let the exception put the message into the error queue.
                }
            }
        }

        /// <summary>
        /// If needed, attempt to move the .docs file to an archive folder.
        /// </summary>
        private Task ArchiveRedCardDocsFile(DS.Edi835 edi835)
        {
            // If it's not set to archive, or the archive root folder doesn't exist, or it's not RedCard we're done here.
            if (!_settings.ArchiveRedCardDocsAfterProcessing
                || (!_fileSystem.Directory.Exists(_settings.DocsArchiveFolder))
                || edi835.OriginCode != "RC")
            {
                return Task.CompletedTask;
            }
            // Make sure the .docs file exists in the DocFolder... and there's not a lock file on it
            var filename = $"{edi835.UserKey}_DD_0000000000.docs";
            var docsFilename = _fileSystem.Path.Combine(_settings.DocsFolder, filename);
            var archiveFolder = _settings.GetRedCardDocsArchiveFolder(edi835.LoadDate);
            if ( !_fileSystem.Directory.Exists(archiveFolder))
            {
                _logger.LogInformation(".docs archive folder does not exist for {docsArchiveFolder}" , archiveFolder);
                _fileSystem.Directory.CreateDirectory(archiveFolder);
            }
            var archivedDocsFilename = _fileSystem.Path.Combine(archiveFolder, filename);
            var docsFilenameLocked = docsFilename + ".lock";
            if (!_fileSystem.File.Exists(docsFilename))
            {
                _logger.LogInformation(".docs file not found for archiving - {docsFilename} {TransactionId} {UserKey}"
                    , docsFilenameLocked, edi835.TransactionId, edi835.UserKey);
                return Task.CompletedTask;
            }
            if (_fileSystem.File.Exists(archivedDocsFilename))
            {
                _logger.LogInformation(".docs file already archived - {docsFilename} {TransactionId} {UserKey}"
                    , docsFilenameLocked, edi835.TransactionId, edi835.UserKey);
                return Task.CompletedTask;
            }
            if (_fileSystem.File.Exists(docsFilenameLocked))
            {                _logger.LogInformation(".docs file locked by another process - {docsFilename} {TransactionId} {UserKey}"
                    , docsFilenameLocked, edi835.TransactionId, edi835.UserKey);
                return Task.CompletedTask;
            }
            try
            {
                _fileSystem.File.Move(docsFilename, docsFilenameLocked);
                _fileSystem.File.Move(docsFilenameLocked, archivedDocsFilename);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ".docs file archiving failed with unexpected error - {docsFilename} {TransactionId} {UserKey}"
                    , docsFilenameLocked, edi835.TransactionId, edi835.UserKey);
            }

            return Task.CompletedTask;
        }

        private bool IsSkipOpach(Exception ex)
        {
            return (ex is OpachBankingDetailsMissingException) ||
                (ex is PdfFileNotFoundException pdfEx
                    && string.Equals(pdfEx.Edi835.PaymentTypeCode, PayType.OPACH, StringComparison.OrdinalIgnoreCase)
                    && pdfEx.Message.Contains("Unable to find Archive"));
        }

        private string WriteTarToFolder(TarPackage tarPackage)
        {
            var outboundTarFolder = _fileSystem.Path.Combine(_settings.OutboundTarFolder, DateTime.Now.ToString("yyyy/MM/dd/"));

            _fileSystem.Directory.CreateDirectory(outboundTarFolder);

            var outboundTarFilename = _fileSystem.Path.Combine(outboundTarFolder, tarPackage.Filename);

            if (_fileSystem.File.Exists(outboundTarFilename))
            {
                throw new IOException($"Outbound tar file already exists {outboundTarFilename}");
            }

            using (var fs = _fileSystem.File.Create(outboundTarFilename))
            {
                ((MemoryStream)tarPackage.Stream).WriteTo(fs);
                fs.Flush();
            }
            return outboundTarFilename;
        }

        private async Task TransferTarFile(string localFilePath, CancellationToken cancellationToken)
        {
            var remoteHost = _settings.TarFileTransfer.Host;
            var remoteFilePath = _fileSystem.Path.Combine(_settings.TarFileTransfer.TransferPath, _fileSystem.Path.GetFileName(localFilePath));
            var userName = _settings.TarFileTransfer.UserName;
            var password = _settings.TarFileTransfer.Password;

            await _fileTransferService.TransferFile(
               localPath: localFilePath,
               remoteHost: remoteHost,
               remotePath: remoteFilePath,
               userName: userName,
               password: password,
               cancellationToken: cancellationToken);
        }

        private DS.Edi835 UnwrapMessage(string message)
        {
            try
            {
                var deserialized = JsonConvert.DeserializeObject<MessageWrapper>(message);
                return JsonConvert.DeserializeObject<DS.Edi835>(Convert.ToString(deserialized.Message));
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Edi835 message does not appear to have MessageWrapper. Message: {Message}", message);
            }
            // Messages _can_ be placed on the queue without the message wrapper
            return JsonConvert.DeserializeObject<DS.Edi835>(message);
        }
    }
}
