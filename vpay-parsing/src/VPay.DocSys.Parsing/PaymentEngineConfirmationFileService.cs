using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;

namespace VPay.DocSys.Parsing;

/// <summary>
/// Provides methods for processing Payment Engine Confirmation sts files
/// </summary>
public class PaymentEngineConfirmationFileService : IPaymentEngineConfirmationFileService
{
    private readonly IFileSystem _fileSystem;
    private readonly IPlatformConfigurationApi _platformConfigurationApi;
    private readonly ISystemClock _systemClock;
    private readonly IPaymentEngineConfirmationFileSender _paymentEngineConfirmationFileSender;
    private readonly ILogger<PaymentEngineConfirmationFileService> _logger;

    public PaymentEngineConfirmationFileService(
        IFileSystem fileSystem,
        IPlatformConfigurationApi platformConfigurationApi,
        ISystemClock systemClock,
        IPaymentEngineConfirmationFileSender paymentEngineConfirmationFileSender,
        ILogger<PaymentEngineConfirmationFileService> logger
        )
    {
        ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
        ArgumentHelpers.ThrowIf.IsNull(platformConfigurationApi, nameof(platformConfigurationApi));
        ArgumentHelpers.ThrowIf.IsNull(systemClock, nameof(systemClock));
        ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

        _fileSystem = fileSystem;
        _platformConfigurationApi = platformConfigurationApi;
        _systemClock = systemClock;
        _paymentEngineConfirmationFileSender = paymentEngineConfirmationFileSender;
        _logger = logger;
    }

    private static bool IsProd()
    {
        var appEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower();
        return appEnv is "prod" or "production";
    }

    public Task ProcessFile(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIf.IsNullOrEmpty(filePath, nameof(filePath));

        var message = new PaymentEngineConfirmationMessage(filePath);

        return ProcessFile(message, cancellationToken);
    }

    public async Task ProcessFile(PaymentEngineConfirmationMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));
        ArgumentHelpers.ThrowIf.IsNullOrEmpty(message.FilePath, nameof(message), nameof(message.FilePath));

        using (_logger.BeginScope(message))
        {
            try
            {
                _logger.LogInformation("Processing Payment Engine confirmation file: {FileName}", message.FilePath.Replace(Environment.NewLine, ""));

                if (!_fileSystem.File.Exists(message.FilePath))
                {
                    throw new FileNotFoundException("File not found.", message.FilePath);
                }

                var stsFile = new StatusFile(await _fileSystem.File.ReadAllLinesAsync(message.FilePath, cancellationToken));
                var stsPath = _fileSystem.Path.GetDirectoryName(message.FilePath) ?? "";
                var statusFileData = new List<string>();
                const int Status = 1; // This should always be 1 if it's in the STS file.

                foreach (var stsGroupByData in stsFile.Records.GroupBy(x => new { x.TPA, x.BILENT }))
                {
                    var clientCode = stsGroupByData.Key.TPA;
                    var billingEnt = stsGroupByData.Key.BILENT;

                    _logger.LogInformation("Processing TPA:{ClientCode}, BE:{BillingEntityCode} from: {FileName}", clientCode, billingEnt, message.FilePath.Replace(Environment.NewLine, ""));

                    var mappingInfo = await _platformConfigurationApi.GetMappingByClientCodeAndBillingEntityCode(clientCode, billingEnt, cancellationToken).ConfigureAwait(false);

                    var fileName = $"{clientCode}_Trigger_{mappingInfo.OptumSubPayorId}_{_systemClock.Now:yyyy-MM-dd.hh.mm.ss.fffffff}.txt";
                    if (!IsProd())
                    {
                        fileName = "NP_" + fileName;
                    }

                    statusFileData.AddRange(stsGroupByData.Select(x => $"{x.TXID}~{x.USERKEY}~{mappingInfo.OptumPayorId}~{mappingInfo.OptumSubPayorId}~{Status}"));

                    var peConfirmationFileName = _fileSystem.Path.Combine(stsPath, fileName);

                    await _fileSystem.File.WriteAllLinesAsync(peConfirmationFileName, statusFileData, cancellationToken);

                    _logger.LogInformation("Sending to external file transfer service: {FileName}", message.FilePath.Replace(Environment.NewLine, ""));

                    await _paymentEngineConfirmationFileSender.SendFileToTransferService(_fileSystem.FileInfo.New(peConfirmationFileName), fileName, cancellationToken);
                }

                _logger.LogInformation("Processing payment engine confirmation file: {FileName} completed.", message.FilePath.Replace(Environment.NewLine, ""));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Processing payment engine confirmation file: {FileName} failed: {Message}", message.FilePath.Replace(Environment.NewLine, ""), exception.Message);
                throw;
            }
        }
    }
}
