using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.Distribution.Common
{
    public class AuxiliaryFileProcessor : IAuxiliaryFileProcessor
    {
        private readonly ISshOperations _sshOperations;
        private readonly AuxiliaryScpSettings _auxiliaryScpSettings;
        private readonly IRedCardRepository _redCardRepository;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<AuxiliaryFileProcessor> _logger;

        public AuxiliaryFileProcessor(
            ISshOperations sshOperations,
            AuxiliaryScpSettings auxiliaryScpSettings,
            IRedCardRepository redCardRepository,
            IFileSystem fileSystem,
            ILogger<AuxiliaryFileProcessor> logger)
        {
            _sshOperations = sshOperations;
            _auxiliaryScpSettings = auxiliaryScpSettings;
            _redCardRepository = redCardRepository;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public  async Task<RedCardProcessResult> Process(AuxiliaryMessage message)
        {
            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                {"STSFile", message.StatusFileName}
            }))
            {
                if ((await _redCardRepository.GetFileStatus(message.PaymentStatusFileId)) >= RedCardFileStatusCode.AuxiliaryFileTransferred)
                {
                    _logger.LogAndThrowError($".AUX already sent for file {message.StatusFileName}.");
                }

                try
                {
                    var auxiliaryFile = message.StatusFileName.GetAuxiliaryFilePathFromStatusFilePath();
                    var controlFile = $"{_fileSystem.Path.GetFileName(auxiliaryFile)}.ctrl";
                    
                    var result = new RedCardProcessResult();

                    if (_auxiliaryScpSettings.TransferFiles)
                    {
                        await _sshOperations.TransferFileAsync(auxiliaryFile, _auxiliaryScpSettings);
                        await _sshOperations.TouchControlFileOnServerAsync(controlFile, _auxiliaryScpSettings);

                        result.FileNames.Add(auxiliaryFile);
                        result.ControlFileNames.Add(controlFile);
                    }

                    _logger.LogInformation($"Updating RedCardFileStatus record to {nameof(RedCardFileStatusCode.AuxiliaryFileTransferred)} for {message.StatusFileName}");
                    await _redCardRepository.UpdateFileStatus(message.PaymentStatusFileId, RedCardFileStatusCode.AuxiliaryFileTransferred);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    throw;
                }
            }
        }
    }
}
