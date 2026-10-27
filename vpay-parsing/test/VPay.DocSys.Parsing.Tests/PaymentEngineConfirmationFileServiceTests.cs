using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Refit;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using VPay.DocSys.Parsing.QueueProcessor.Consolidated;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;
using Xunit;

namespace VPay.DocSys.Parsing.Tests;

public class PaymentEngineConfirmationFileServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly IServiceCollection _serviceCollection;

    private readonly Mock<ISystemClock> _systemCock = new();
    private readonly Mock<ISigningService> _signingServiceMock = new();
    private readonly Mock<IFileReadyForTransferNotificationPublisher> _fileReadyForTransferNotificationPublisherMock = new ();
    private readonly DateTime _testDateTimeNow = new DateTime(2023, 1, 11, 2, 50, 10);
    protected static string[] StatusFileData => File.ReadAllLines("statusFileTestData/B001809287_FS3U201935.sts");
    private const string PrivateKeyPath = "/some/test/path/key.ext";

    public PaymentEngineConfirmationFileServiceTests()
    {
        _systemCock.Setup(x => x.Now).Returns(_testDateTimeNow);

        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile("appsettings.Development.json", true)
            .Build();
        _serviceCollection = new ServiceCollection()
            .AddSingleton(_configuration)
            .AddLogging(b => b.AddProvider(NullLoggerProvider.Instance))
            .AddServices(_configuration)
            .AddSingleton(_signingServiceMock.Object)
            .AddSingleton(_fileReadyForTransferNotificationPublisherMock.Object)
            .AddSingleton(_systemCock.Object);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProcessMessage_ShouldCreateFileAsExpected(bool production)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", production ? "prod" : "dev");
        var testFilePath = "statusFileTestData/B001809287_FS3U201935.sts";
        var clientCode = "WEB";
        var billingEntityCode = "WEB0001044";
        var optumPayorId = "99999";
        var optumSubPayorId = "000000001";

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { testFilePath, new MockFileData(File.ReadAllText(testFilePath)) }
        });
        _serviceCollection.AddSingleton<IFileSystem>(fileSystem);

        var signingServiceOptionsMock = new Mock<IOptions<SigningServiceOptions>>();
        signingServiceOptionsMock.Setup(x => x.Value)
            .Returns(new SigningServiceOptions() { PrivateKeyPath = PrivateKeyPath });
        _serviceCollection.AddSingleton(signingServiceOptionsMock.Object);

        var platformConfigApiMock = new Mock<IPlatformConfigurationApi>();
        platformConfigApiMock.Setup(x =>
                x.GetMappingByClientCodeAndBillingEntityCode(
                    It.Is<string>(p => p.Equals(clientCode)),
                    It.Is<string>(p => p.Equals(billingEntityCode)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrsMappingResult
            {
                OptumPayorId = optumPayorId,
                OptumSubPayorId = optumSubPayorId
            });
        _serviceCollection.AddSingleton(platformConfigApiMock.Object);

        _signingServiceMock.Setup(x => x.Sign(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("signedHash");

        var serviceProvider = _serviceCollection.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var cachedPlatformConfigApi = serviceProvider.GetRequiredService<IPlatformConfigurationApi>();

        _fileReadyForTransferNotificationPublisherMock.Setup(x =>
                x.Publish(It.IsAny<FileReadyForTransferNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expectedFileName = $"{(production ? "" : "NP_")}{clientCode}_Trigger_{optumSubPayorId}_{_testDateTimeNow:yyyy-MM-dd.hh.mm.ss.fffffff}.txt";
        expectedFileName = fileSystem.Path.Combine("statusFileTestData", expectedFileName);

        var fileService = serviceProvider.GetRequiredService<IPaymentEngineConfirmationFileService>();
        await fileService.ProcessFile(testFilePath);

        _fileReadyForTransferNotificationPublisherMock.Verify(x => x.Publish(It.IsAny<FileReadyForTransferNotification>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.True(fileSystem.File.Exists(expectedFileName));

        var fileData = await fileSystem.File.ReadAllLinesAsync(expectedFileName);
        Assert.Equal(4, fileData.Length);
        Assert.Equal($"00000000000770376643~1085001TX2020032709000002A202000432452542~{optumPayorId}~{optumSubPayorId}~1", fileData[0]);
        Assert.Equal($"00000000000770376673~1085001TX2020032709000009A202000432454641~{optumPayorId}~{optumSubPayorId}~1", fileData[1]);
        Assert.Equal($"00000000000770376701~1085001TX2020032709000015A202000432455148~{optumPayorId}~{optumSubPayorId}~1", fileData[2]);
        Assert.Equal($"00000000000770376781~1085001TX2020032709000031A202000432453790~{optumPayorId}~{optumSubPayorId}~1", fileData[3]);
    }

    [Fact]
    public async Task ProcessMessage_STSFileMissing_ThrowsException()
    {
        var platformConfigApiMock = new Mock<IPlatformConfigurationApi>();
        platformConfigApiMock.Setup(x =>
                x.GetMappingByClientCodeAndBillingEntityCode(It.Is<string>(p => p.Equals("WEB")),
                    It.Is<string>(p => p.Equals("WEB0001044")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrsMappingResult { OptumPayorId = "99999", OptumSubPayorId = "000000001" });
        _serviceCollection.AddSingleton(platformConfigApiMock.Object);

        var serviceProvider = _serviceCollection.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var cachedPlatformConfigApi = serviceProvider.GetRequiredService<IPlatformConfigurationApi>();

        var fileService = serviceProvider.GetRequiredService<IPaymentEngineConfirmationFileService>();
        await Assert.ThrowsAsync<FileNotFoundException>(() => fileService.ProcessFile("statusFileTestData/NOT_FOUND.sts"));
    }

    [Fact]
    public async Task ProcessMessage_UnableToFindClientBE_ThrowsException()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not Found")
        };
        var platformConfigApiMock = new Mock<IPlatformConfigurationApi>();
        platformConfigApiMock.Setup(x =>
                x.GetMappingByClientCodeAndBillingEntityCode(It.Is<string>(p => p.Equals("WEB")),
                    It.Is<string>(p => p.Equals("WEB0001044")), It.IsAny<CancellationToken>()))
            .Throws(await ApiException.Create(
                "404",
                new HttpRequestMessage(HttpMethod.Get, new Uri("https://www.google.com")),
                HttpMethod.Get,
                expectedResponse,
                null));
        _serviceCollection.AddSingleton(platformConfigApiMock.Object);

        var serviceProvider = _serviceCollection.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var cachedPlatformConfigApi = serviceProvider.GetRequiredService<IPlatformConfigurationApi>();

        var fileService = serviceProvider.GetRequiredService<IPaymentEngineConfirmationFileService>();
        await Assert.ThrowsAsync<ApiException>(() => fileService.ProcessFile("statusFileTestData/B001809287_FS3U201935.sts"));
    }
}
