using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Api.Controllers;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using Xunit;

namespace VPay.DocSys.Parsing.Api.Tests
{
    public class ConfigControllerTests
    {
        private readonly Mock<IDocSysConfigService> _configServiceMock = new Mock<IDocSysConfigService>();
        private readonly Mock<IXmlConfigParserService> _xmlconfigParserServiceMock = new Mock<IXmlConfigParserService>();
        private readonly Mock<IPlatformConfigurationApi> _platformConfigurationApiMock = new Mock<IPlatformConfigurationApi>();
        private readonly Mock<ILogger<ConfigController>> _loggerMock = new Mock<ILogger<ConfigController>>();

        private readonly ConfigController _sut;

        public ConfigControllerTests()
        {
            _sut = new ConfigController(
                _configServiceMock.Object,
                _xmlconfigParserServiceMock.Object,
                _platformConfigurationApiMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void Constructor_DefaultServiceCollection_ShouldReturnInstance()
        {
            // arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.Development.json", true).Build();
            var collection = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
            VPay.DocSys.Parsing.DependencyInjectionHelper
                .ConfigureDependencies(collection, configuration)
                .AddTransient<ConfigController>();

            // act
            var provider = collection.BuildServiceProvider();
            var controller = provider.GetRequiredService<ConfigController>();

            // assert
            Assert.NotNull(controller);
        }

        [Fact]
        public void ReadConfig_ConfigurationServiceReturnsNull_ShouldReturnNotFound()
        {
            _configServiceMock.Setup(x => x.ReadConfig("tpa")).Returns((DocSysConfiguration)null);

            var actual = _sut.ReadConfig("tpa");

            actual.Should()
                .BeOfType<NotFoundResult>();
        }

        [Fact]
        public void ReadConfig_ConfigurationServiceReturnsObject_ShouldReturnOkWithObject()
        {
            var expected = new DocSysConfiguration();

            _configServiceMock.Setup(x => x.ReadConfig("tpa")).Returns(expected);

            var actual = _sut.ReadConfig("tpa");

            actual.Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData("TPA", false)]
        [InlineData("TPA", true)]
        public async Task GetCustomSendBackClientConfigurations_ShouldReturnExpected(string given, bool expected)
        {
            // arrange
            var expectedPagination = new Pagination<CustomSendBackClientBoardingConfigDto>(
                new CustomSendBackClientBoardingConfigDto
                {
                    Status = ClientBoardingConfigDtoBase.ACTIVE,
                    OriginCode = "O",
                    ClientCode = given,
                    IsCustomSendBack = expected
                },
                new CustomSendBackClientBoardingConfigDto
                {
                    Status = ClientBoardingConfigDtoBase.ACTIVE,
                    OriginCode = ClientBoardingConfigDtoBase.DEFAULT,
                    ClientCode = "",
                    IsCustomSendBack = false,
                });
            _platformConfigurationApiMock
                .Setup(m => m.GetCustomSendBackByClient(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedPagination);

            // act
            var act = await _sut.ReadIsCustomSendBackEnabledForClient(given, false).ConfigureAwait(false);

            // assert
            act.Should().NotBeNull();
            act.Should().BeOfType<OkObjectResult>();
            act.As<OkObjectResult>().Value.Should().BeOfType<bool>();
            act.As<OkObjectResult>()
                .Value.As<bool>().Should().Be(expected);
            _platformConfigurationApiMock
                .Verify(m => m.GetCustomSendBackByClient(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("TPA")]
        public async Task ReadEnabledCustomSendBackClientCodes_ShouldReturnExpected(params string[] expected)
        {
            // arrange
            _platformConfigurationApiMock
                .Setup(m => m.GetCustomSendBackByClient(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pagination<CustomSendBackClientBoardingConfigDto>(
                    expected.Select(x => new CustomSendBackClientBoardingConfigDto { Status = ClientBoardingConfigDtoBase.ACTIVE, ClientCode = x, IsCustomSendBack = true })));

            // act
            var act = await _sut.ReadCustomSendBackEnabledClientCodes(false).ConfigureAwait(false);

            // assert
            act.Should().NotBeNull();
            act.Should().BeOfType<OkObjectResult>();
            act.As<OkObjectResult>()
                .Value.As<HashSet<string>>().Should().BeEquivalentTo(expected.ToList());
            _platformConfigurationApiMock
                .Verify(m => m.GetCustomSendBackByClient(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReadEnabledCustomSendBackClientCodes_ShouldReturnNotFound()
        {
            // arrange
            var expected = new string[] { "TPA" };
            _platformConfigurationApiMock
                .Setup(m => m.GetCustomSendBackByClient(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pagination<CustomSendBackClientBoardingConfigDto>(
                    expected.Select(x => new CustomSendBackClientBoardingConfigDto { ClientCode = x, IsCustomSendBack = false })));

            // act
            var act = await _sut.ReadCustomSendBackEnabledClientCodes(false).ConfigureAwait(false);

            // assert
            act.Should().NotBeNull();
            act.Should().BeOfType<NotFoundResult>();
            act.As<NotFoundResult>()
                .StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            _platformConfigurationApiMock
                .Verify(m => m.GetCustomSendBackByClient(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("TLC", "TLC0001001", EdiDeliveryDestination.Availity, EdiDeliveryFileExtension.Edi835)]
        [InlineData("WEB", "", EdiDeliveryDestination.Availity, EdiDeliveryFileExtension.EdiBRN)]
        public async Task ReadIsEdiDeliveryEnabledForClient_ShouldReturnExpected(string client, string bilingEntity, EdiDeliveryDestination delivery, EdiDeliveryFileExtension extension)
        {
            // arrange
            _platformConfigurationApiMock
                .Setup(m => m.GetEdiDeliveryByClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pagination<EdiDeliveryClientBoardingConfigDto>(
                    new EdiDeliveryClientBoardingConfigDto
                    {
                        BeginDate = DateTime.Today.AddYears(-5),
                        EndDate = DateTime.Today.AddYears(5),
                        Status = ClientBoardingConfigDtoBase.ACTIVE,
                        OriginCode = "O",
                        ClientCode = client,
                        BillingEntityCode = bilingEntity,
                        Destination = delivery,
                        FileExtension = extension
                    }));

            // act
            var act = await _sut.ReadEdiDeliveryForClient(client, bilingEntity, false).ConfigureAwait(false);

            // assert
            act.Should().NotBeNull();
            act.Should().BeOfType<OkObjectResult>();
            act.As<OkObjectResult>()
                .StatusCode.Should().Be((int)HttpStatusCode.OK);
            var response = act.As<OkObjectResult>().Value.As<IEnumerable<EdiDeliveryClientBoardingConfigDto>>();
            response.First().Destination.Should().Be(delivery);
            response.First().FileExtension.Should().Be(extension);
            _platformConfigurationApiMock
                .Verify(m => m.GetEdiDeliveryByClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("TLC", "TESTKEY", "TLC0001001", "RC", "CLCHK")]
        public async Task ReadConfigurationKeyForClient_ShouldReturnExpected(string client, string key, string bilingEntity, string origin, string paymentType)
        {
            // arrange
            var expected = new Pagination<ClientBoardingConfigDto>(
                    new ClientBoardingConfigDto
                    {
                        Status = ClientBoardingConfigDtoBase.ACTIVE,
                        ClientCode = client,
                        BillingEntityCode = bilingEntity,
                        OriginCode = origin,
                        PaymentTypeCode = paymentType
                    });
            _platformConfigurationApiMock
                .Setup(m => m.GetClientBoardingConfigs<ClientBoardingConfigDto>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                                                                                It.IsAny<System.DateTimeOffset?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            // act
            var act = await _sut.ReadConfigurationKeyForClient(client, key, bilingEntity, origin, paymentType).ConfigureAwait(false);

            // assert
            act.Should().NotBeNull();
            act.Should().BeOfType<OkObjectResult>();
            act.As<OkObjectResult>()
                .StatusCode.Should().Be((int)HttpStatusCode.OK);
            act.As<OkObjectResult>()
                .Value.As<List<ClientBoardingConfigDto>>().Should().BeEquivalentTo(expected.Items.ToList());
            _platformConfigurationApiMock
                .Verify(m => m.GetClientBoardingConfigs<ClientBoardingConfigDto>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                                                                                It.IsAny<System.DateTimeOffset?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
