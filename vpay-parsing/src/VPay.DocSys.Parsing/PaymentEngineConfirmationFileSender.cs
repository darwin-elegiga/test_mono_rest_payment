using System;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;

namespace VPay.DocSys.Parsing;

public class PaymentEngineConfirmationFileSender : IPaymentEngineConfirmationFileSender
{
    private const string FileTransferServiceFolder = "/working";
    private const string FileTransferServiceSubFolder = "pe_confirmation";
    private const string Source = "OPTUM_PE"; 
    private const string Destination = "PE";

    private readonly IFileSystem _fileSystem;
    private readonly ISigningService _signingService;
    private readonly IOptions<SigningServiceOptions> _signingServiceOptions;
    private readonly IHashingService<SHA256CryptoServiceProvider> _hashingService;
    private readonly IFileReadyForTransferNotificationPublisher _fileReadyForTransferNotificationPublisher;
    private readonly ILogger<IPaymentEngineConfirmationFileSender> _logger;

    public PaymentEngineConfirmationFileSender(
        IFileSystem fileSystem,
        ISigningService signingService,
        IOptions<SigningServiceOptions> signingServiceOptions,
        IHashingService<SHA256CryptoServiceProvider> hashingService,
        IFileReadyForTransferNotificationPublisher fileReadyForTransferNotificationPublisher,
        ILogger<IPaymentEngineConfirmationFileSender> logger)
    {
        _fileSystem = fileSystem;
        _signingService = signingService;
        _signingServiceOptions = signingServiceOptions;
        _hashingService = hashingService;
        _fileReadyForTransferNotificationPublisher = fileReadyForTransferNotificationPublisher;
        _logger = logger;
    }

    public async Task SendFileToTransferService(IFileInfo fileInfo, string trackingId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received File Transfer Request for {FileName}", fileInfo.FullName);
        // copy the file to the file transfer service directory
        var transferServiceFileInfo = CopyFileToTransferServiceLocation(fileInfo);

        // get the path to the file relative to the base directory
        var fileLocation =
            _fileSystem.Path.GetRelativePath(FileTransferServiceFolder, transferServiceFileInfo.FullName);

        // calculate the hash for the file and sign it
        await using var fileStream = fileInfo.OpenRead();

        var hash = _hashingService.ComputeHash(fileStream);
        _logger.LogInformation("Signing the File Transfer Request for {FileName}", fileInfo.FullName);

        // SEE: Generate signing keys for additional information about how to do this
        var signedHash = await _signingService.Sign(
            _signingServiceOptions.Value.PrivateKeyPath,
            hash,
            cancellationToken
        ).ConfigureAwait(false);

        _logger.LogInformation("Hash Generated for {FileName}: [{FileHash}]", fileInfo.FullName, signedHash);

        // add a custom property that will flow through with our message
        var additionalProperties = new JObject(new JProperty("trackingId", trackingId));

        _logger.LogInformation("Notifying the External File Transfer System to send the file: {FileName}",
            fileInfo.FullName);

        // fire the notification so we can perform additional actions for the file
        await _fileReadyForTransferNotificationPublisher.Publish(
            new FileReadyForTransferNotification(Source, Destination, fileLocation, signedHash,
                additionalProperties),
            cancellationToken
        ).ConfigureAwait(false);
        _logger.LogInformation("Finished File Transfer Request for {FileName}", fileInfo.FullName);
    }

    private IFileInfo CopyFileToTransferServiceLocation(IFileInfo fileInfo)
    {
        var directory = CreateDatedDirectory(FileTransferServiceFolder, FileTransferServiceSubFolder);
        var actualPath = _fileSystem.Path.Combine(directory, fileInfo.Name);
        return fileInfo.CopyTo(actualPath, overwrite: true);
    }

    private string CreateDatedDirectory(string basePath, string subPath = null)
    {
        var date = DateTime.Today;
        var path = _fileSystem.Path.Combine(
            basePath,
            subPath ?? string.Empty,
            date.Year.ToString(),
            date.Month.ToString("00"),
            date.Day.ToString("00"));

        if (!_fileSystem.Directory.Exists(path))
        {
            _fileSystem.Directory.CreateDirectory(path);
        }

        return path;
    }
}
