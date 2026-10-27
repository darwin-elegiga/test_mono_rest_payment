using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Renci.SshNet.Common;
using VPay.Data.Db2.Abstractions.Transaction;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Inbound835Files;
using VPay.Extensions.Ssh;

namespace VPay.DocSys.Parsing
{
    /// <summary>
    /// An <see cref="IInbound835FileProcessor"/> that processes <see cref="Inbound835FileType.PaymentPosted"/> files.
    /// </summary>
    public class Inbound835PaymentPostedFileProcessor : Inbound835FileProcessor, IInbound835PaymentPostedFileProcessor
    {
        private readonly ITransactionRepository _db2TransactionRepository;
        private readonly PaymentPostedFileSettings _paymentPostedFileSettings;
        private readonly ILogger<Inbound835PaymentPostedFileProcessor> _logger;
        private readonly IAsyncPolicy _db2ProcedurePolicy;

        public Inbound835PaymentPostedFileProcessor(
            IFileSystem fileSystem,
            ISftpService sftpService,
            IEdi835Repository edi835Repository,
            ITransactionRepository db2TransactionRepository,
            IInbound835FileParser inbound835FileParser,
            Inbound835FileServiceSettings inbound835FileServiceSettings,
            ILogger<Inbound835PaymentPostedFileProcessor> logger
            ) : base(fileSystem, sftpService, edi835Repository, inbound835FileParser, logger)
        {
            _db2TransactionRepository = db2TransactionRepository;
            _paymentPostedFileSettings = inbound835FileServiceSettings.PaymentPostedFile;
            _logger = logger;

            // create retry policy used running proc on Db2 to process file
            if(_paymentPostedFileSettings.iSeriesProcess.Enabled && _paymentPostedFileSettings.iSeriesProcess.Retries > 0)
            {
                _db2ProcedurePolicy = Policy
                    .Handle<OdbcException>()
                    .WaitAndRetryAsync(
                        _paymentPostedFileSettings.iSeriesProcess.Retries,
                        (r) => _paymentPostedFileSettings.iSeriesProcess.SleepBetweenRetries,
                        this.OnRetry);
            }
            else
            {
                _db2ProcedurePolicy = Policy.NoOpAsync();
            }
        }

        public override async Task ProcessFile(Inbound835FileMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(message.FilePath, nameof(message), nameof(message.FilePath));

            using(_logger.BeginScope(message, _paymentPostedFileSettings))
            {
                // parse the file
                var pmtPostedFile = await ParsePaymentPostedFile(message.FilePath, cancellationToken);

                // get list of all lines from file (removing the 221 records)                
                var allLines = pmtPostedFile.Lines
                    .Where(line => line.TransactionId != 221)
                    .ToList();

                //  NOTE: Inbound835File messages sometimes appear with a line that is just "221" instead of a valid transactionId.
                //   Historically, whoever is oncall would remove that record and reprocess the message,
                //   and if the only record in the file is a 221 then discard the message entirely
                if (!allLines.Any())
                {
                    // Here we are returning if the only lines are 221 so that it doesn't throw any errors into the queue
                    return;
                }

                // lookup the 835s and build a list of lines for which we have an 835
                var edi835s = await LookupEdi835s(allLines.Select(l => l.TransactionId), cancellationToken);

                // build list of lines for which we have an 835 (list of tuples with line and 835)
                var linesWith835 = edi835s.Select<Edi835, (PaymentPostedFile.Line Line, Edi835 Edi835)>(e => (allLines.Single(l => l.TransactionId == e.TransactionId), e)).ToList();

                // build list of lines for which we have no 835
                var linesWithNo835 = allLines.Except(linesWith835.Select(_ => _.Line)).ToList();

                // if any transactions don't have 835s its and error condition
                if(linesWithNo835.Any())
                {
                    // format a comma separated list of the txn ids that don't have 835s
                    //      example: '[9100001010, 9100001011, 9100001012 (+12 more)]'
                    var maxTake = linesWithNo835.Count <= 3 ? linesWithNo835.Count : 3;
                    var notTaken = linesWithNo835.Count - maxTake;
                    var more = notTaken > 0 ? $" (+{notTaken} more)" : "";
                    var trnsIdsWithNo835AsString = $"[{string.Join(',', linesWithNo835.Take(maxTake).Select(l => l.TransactionId.ToString()))}{more}]";

                    // unless this debugging flag is set in which case we'll ignore transactions missing 835s
                    if(!_paymentPostedFileSettings.IgnoreMissingTransactions)
                    {
                        throw _logger.LogErrorAndReturnException($"No 835 request found for {linesWithNo835.Count} of {allLines.Count} transactions: {trnsIdsWithNo835AsString}");
                    }
                }

                // log how many 835 requests we found
                _logger.LogInformation($"Found 835 requests for {linesWith835.Count} of {allLines.Count} transactions.");

                // if we found any 835s, process those transaction in new system
                if(linesWith835.Count > 0)
                {
                    await ProcessLinesInNewSystem(pmtPostedFile.FullFileName, linesWith835, cancellationToken);
                }
            }
        }

        private async Task ProcessLinesInNewSystem(string originalFileName, IEnumerable<(PaymentPostedFile.Line Line, Edi835 Edi835)> linesAnd835s, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Processing {linesAnd835s.Count()} lines of payment posted file in new system: {originalFileName}");

                // get next file name to write lines to
                var newSystemFileName = await GetNextPaymentPostedFileName(originalFileName, cancellationToken);

                using(_logger.BeginScope(new { FilePathAsProcessed = newSystemFileName }.ToDictionary()))
                {
                    // write lines to new system file
                    await WritePaymentPostedFile(newSystemFileName, linesAnd835s.Select(_ => _.Line), cancellationToken);

                    // transfer file to iSeries
                    if(_paymentPostedFileSettings.iSeriesTransfer.Enabled)
                    {
                        await TransferFileToISeries(newSystemFileName, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation("iSeries file transfer disabled - skipping file transfer.");
                    }

                    // process file on iSeries
                    if(_paymentPostedFileSettings.iSeriesProcess.Enabled)
                    {
                        await ProcessFileOnISeries(newSystemFileName, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation("iSeries file processing disabled - skipping processing.");
                    }

                    // group linesAnd83s by Line.RejectionCode
                    var linesAnd835sGroupedByRejectionCode = linesAnd835s.GroupBy(_ => _.Line.RejectionCode);

                    // update 835 statuses and log rejection codes
                    foreach(var grouping in linesAnd835sGroupedByRejectionCode)
                    {
                        // get grouped 835s
                        var edi835s = grouping.Select(_ => _.Edi835).ToList();

                        // if rejection code is provided
                        if(!string.IsNullOrWhiteSpace(grouping.Key))
                        {
                            // get rejection code data for rejection code
                            var rejectionCode = RejectionCodes.Lookup(grouping.Key);

                            // log rejection code
                            _logger.LogInformation($"{rejectionCode.Description} - {edi835s.Count} transactions");

                            // update 835 statuses
                            await Update835Statuses(edi835s, rejectionCode.Edi835StatusCode, newSystemFileName, $"{edi835s.Count} transaction(s) with rejection code: ({rejectionCode.Value}) - {rejectionCode.Description}", cancellationToken);
                        }
                        else
                        {
                            // otherwise, update the 835 status to VendorPostedPayment
                            await Update835Statuses(edi835s, Edi835StatusCode.VendorPostedPayment, newSystemFileName, $"{edi835s.Count} transaction(s) posted", cancellationToken);
                        }
                    }
                }

                _logger.LogInformation($"Processing {linesAnd835s.Count()} lines of payment posted file in new system completed.");
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Processing {linesAnd835s.Count()} lines of payment posted file in new system failed: {exception.Message}");
                throw;
            }
        }

        private Task<string> GetNextPaymentPostedFileName(string originalFileName, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Determining next file name from '{originalFileName}'");

                var filePath = FileSystem.Path.GetDirectoryName(originalFileName);
                var baseFileName = FileSystem.Path.GetFileName(originalFileName);
                var extensions = new List<string>();
                while(FileSystem.Path.HasExtension(baseFileName))
                {
                    extensions.Add(FileSystem.Path.GetExtension(baseFileName));
                    baseFileName = FileSystem.Path.GetFileNameWithoutExtension(baseFileName);
                }
                extensions.Reverse();

                var index = 0;
                var newFileName = originalFileName;
                while(FileSystem.File.Exists(newFileName))
                {
                    newFileName = FileSystem.Path.Combine(filePath, $"{baseFileName}-{++index:D3}{string.Concat(extensions)}");
                }

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation($"Determining next file name completed: {newFileName}");

                return Task.FromResult(newFileName);
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Determining next file name failed: {exception.Message}");
                throw;
            }
        }

        private async Task TransferFileToISeries(string localFilePath, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Transferring file to iSeries: {localFilePath}");

                var transferHost = _paymentPostedFileSettings.iSeriesTransfer.Host;
                var transferUserName = _paymentPostedFileSettings.iSeriesTransfer.UserName;
                var transferPassword = _paymentPostedFileSettings.iSeriesTransfer.Password;
                var transferPath = _paymentPostedFileSettings.iSeriesTransfer.TransferPath;
                var baseFileName = FileSystem.Path.GetFileName(localFilePath);
                var transferFileName = FileSystem.Path.Combine(transferPath, baseFileName);

                if(_paymentPostedFileSettings.iSeriesTransfer.UseRsaKey)
                {
                    await TransferFileToRemotePath(
                        localPath: localFilePath,
                        remoteHost: transferHost,
                        remotePath: transferFileName,
                        userName: transferUserName,
                        permissions: 666,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await TransferFileToRemotePath(
                        localPath: localFilePath,
                        remoteHost: transferHost,
                        remotePath: transferFileName,
                        userName: transferUserName,
                        password: transferPassword,
                        permissions: 666,
                        cancellationToken: cancellationToken);
                }

                _logger.LogInformation($"Transferring file to iSeries completed.");
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Transferring file to iSeries failed: {exception.Message}");
                throw;
            }
        }

        private async Task ProcessFileOnISeries(string fileName, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Processing file on iSeries: {fileName}");

                fileName = FileSystem.Path.GetFileName(fileName);

                var iSeriesPath = _paymentPostedFileSettings.iSeriesTransfer.TransferPath;

                await _db2ProcedurePolicy.ExecuteAsync(async () =>
                {
                    var result = await _db2TransactionRepository.Process835VendorResponseFile(iSeriesPath, fileName, cancellationToken);

                    if(result.StatusCode != "0000")
                    {
                        throw new Exception($"{result.StatusCode} - {result.StatusDescription}");
                    }

                }, "Processing file on iSeries");

                _logger.LogInformation($"Processing file on iSeries complete.");
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Processing file on iSeries failed: {exception.Message}");
                throw;
            }
        }

        private void OnRetry(Exception exception, TimeSpan sleepDuration, int retryCount, Context context)
        {
            var operation = context.OperationKey ?? "(unknown operation)";

            using(_logger.BeginScope(new Dictionary<string, object>()
            {
                { "RetryCount", retryCount},
                { "SleepDuration", sleepDuration },
                { "Operation", operation },
            }))
            {
                _logger.LogInformation(exception, $"Retry ({retryCount}): {operation}");
            }
        }
    }
}
