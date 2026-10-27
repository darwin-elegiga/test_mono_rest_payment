using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Common.Rabbitmq;
using VPay.DocSys.Storage;
using VPay.Queuing;

namespace VPay.DocSys.Parsing
{
    public class OutboundRedCardDataDriverFileService : IOutboundRedCardDataDriverFileService, IDriverFileService
    {
        private readonly IConfiguration _configuration;
        private readonly IOutboundRedCardDataDriverFileProcessor _outboundRedCardDataDriverFileProcessor;
        private readonly ILogger<OutboundRedCardDataDriverFileService> _logger;
        private readonly ILogger<IMessagePublisher> _loggerMessagePub;
        private readonly IFileSystem _fileSystem;
        private readonly IArchiveService _archiveService;
        private readonly IRedCardRepository _redCardRepository;
        private readonly IDriverFileService _driverFileService;

        public OutboundRedCardDataDriverFileService(
            IConfiguration configuration,
            IOutboundRedCardDataDriverFileProcessor outboundRedCardDataDriverFileProcessor,
            ILoggerFactory loggerFactory,
            IArchiveService archiveService,
            IRedCardRepository redCardRepository,
            IDriverFileService driverFileService,
            IFileSystem fileSystem)
        {
            _configuration = configuration;
            _outboundRedCardDataDriverFileProcessor = outboundRedCardDataDriverFileProcessor;
            _logger = loggerFactory.CreateLogger<OutboundRedCardDataDriverFileService>();
            _loggerMessagePub = loggerFactory.CreateLogger<IMessagePublisher>();
            _fileSystem = fileSystem;
            _archiveService = archiveService;
            _redCardRepository = redCardRepository;
            _driverFileService = driverFileService;
        }

        /// <summary>
        /// Parse the outbound redcard data driver file
        /// </summary>
        /// <param name="outboundRedCardDataDriverFileName"></param>
        /// <returns>result of process the file</returns>
        public async Task<RedCardProcessResult> ParseFile(string outboundRedCardDataDriverFileName)
        {
            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                {"OutboundRedCardDataDriverFileName", outboundRedCardDataDriverFileName}
            }))
            {
                if(!_driverFileService.GetLock(outboundRedCardDataDriverFileName))
                {
                    _logger.LogInformation($"File has already been processed or is currently being processed by another consumer.");
                    return new RedCardProcessResult();
                }

                try
                {
                    _logger.LogInformation($"Start Processing {outboundRedCardDataDriverFileName.Replace(Environment.NewLine, "")}");

                    ArgumentHelpers.ThrowIf.IsNullOrEmpty(outboundRedCardDataDriverFileName, nameof(outboundRedCardDataDriverFileName));

                    var lines = _fileSystem.File.ReadLines(outboundRedCardDataDriverFileName).ToArray();

                    var driverFile = new OutboundRedCardDataDriverFile(lines);

                    if(driverFile.Records.Count <= 0)
                    {
                        _logger.LogWarning($"Outbound Red Card Data File contains no records.");
                        return new RedCardProcessResult();
                    }

                    var result = await ProcessOutboundRedCardDataDriver(driverFile, outboundRedCardDataDriverFileName);

                    if(result.ResultCode == RedCardProcessResultCode.Error)
                    {
                        _logger.LogError($"An error occurred processing OutboundRedCardDataDriver: {result.Description}");
                    }
                    else if(result.ResultCode == RedCardProcessResultCode.Retry)
                    {
                        _logger.LogInformation($"Unable to process OutboundRedCardDataDriver now ({result.Description}) - try again later.");
                    }
                    else
                    {
                        _logger.LogInformation("Completed Processing {FilePath}", outboundRedCardDataDriverFileName.Replace(Environment.NewLine, ""));

                        _driverFileService.WriteLogFile(outboundRedCardDataDriverFileName, $"{result.RecordsProcessed} records processed");

                        foreach(var f in result.FileNames)
                        {
                            _logger.LogInformation($"OutboundRedCardDataDriver File {f} created");
                        }
                        foreach(var f in result.ControlFileNames)
                        {
                            _logger.LogInformation($"OutboundRedCardDataDriver Control File {f} created");
                        }
                    }

                    return result;
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "An unexpected exception occurred");
                    return new RedCardProcessResult(RedCardProcessResultCode.Error, ex.Message);
                }
                finally
                {
                    _driverFileService.ReleaseLock(outboundRedCardDataDriverFileName);
                }
            }
        }

        public Task QueueFileForParsing(string outboundRedCardDataDriverFileName)
        {
            var queueSettings = new QueueConfigurationSettings();
            _configuration.Bind("OutboundRedCardDataQueue", queueSettings);

            var msg = new RedCardParsingMessage
            {
                FilePath = outboundRedCardDataDriverFileName
            };

            using (var msgQueue = QuorumPublisherFactory.GetPublisher(queueSettings, _loggerMessagePub))
            {
                msgQueue.Publish(JsonConvert.SerializeObject(msg));
                _logger.LogDebug($"Queued outboundRedCardDataDriverFilePath={msg.FilePath.Replace(Environment.NewLine, "")}");
            }

            return Task.CompletedTask;
        }

        private async Task<RedCardProcessResult> ProcessOutboundRedCardDataDriver(OutboundRedCardDataDriverFile driverFile, string outboundRedCardDataDriverFileName)
        {
            // NOTE: 	This code is not intuitive or self-documenting.  It reads only the first record
            //			from the driver file and attempts to get the transaction from the MSSQL database.
            //          if it does NOT find the transaction, it assumes the driver file is associated with
            //			a .sts file that was processed by the legacy docsys so it routes the processing
            //          of the driver file to the legacy docsys.  otherwise, it packages up the records and
            //          call the OutboundRedCardDataDriverFileProcessor.


            // get the first record from the driver file
            var firstRecord = driverFile.Records.FirstOrDefault();
            if(firstRecord == null)
            {
                return new RedCardProcessResult(RedCardProcessResultCode.Error, "Outbound Data Driver contains no records.");
            }

            // attempt to get the transaction from the MSSQL database
            var transaction = await _redCardRepository.GetDocumentTransaction(firstRecord.TransactionID);
            if(transaction == null)
            {
                return new RedCardProcessResult(RedCardProcessResultCode.Error, $"No document transaction record found for (guid: '{firstRecord.Guid}'; transId: {firstRecord.TransactionID})");
            }

            // package up driver file meta data...
            var message = new OutboundRedCardDataDriverMessage()
            {
                DataDriverFileName = outboundRedCardDataDriverFileName,
                DriverFile = driverFile,
                PaymentStatusFileId = transaction.PaymentStatusFileID,
            };

            // call the OutboundRedCardDataDriverFileProcessor
            return await _outboundRedCardDataDriverFileProcessor.Process(message);
        }


        #region [-- implements IDriverFileService --]

        bool IDriverFileService.IsLocked(string driverFile) => _driverFileService.IsLocked(driverFile);

        bool IDriverFileService.GetLock(string driverFile) => _driverFileService.GetLock(driverFile);

        bool IDriverFileService.ReleaseLock(string driverFile) => _driverFileService.ReleaseLock(driverFile);

        bool IDriverFileService.WriteLogFile(string driverFile, string content) => _driverFileService.WriteLogFile(driverFile, content);

        bool IDriverFileService.DeleteLogFile(string driverFile) => _driverFileService.DeleteLogFile(driverFile);

        string IDriverFileService.GetLockFileName(string driverFile) => _driverFileService.GetLockFileName(driverFile);

        string IDriverFileService.GetLogFileName(string driverFile) => _driverFileService.GetLogFileName(driverFile);

        #endregion
    }
}
