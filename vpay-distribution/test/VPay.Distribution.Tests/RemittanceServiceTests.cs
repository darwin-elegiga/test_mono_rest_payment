using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Distribution.Common;
using VPay.Distribution.Http.Abstractions;
using VPay.DocSys.Storage;
using Xunit;

namespace VPay.Distribution.Tests
{
    public class RemittanceServiceTests
    {
        private readonly IRemittancePartnerNetworkApiClient _remitApiClient;
        private readonly IRemitPartnerNetworkApi _remitPartnerNetworkApi;
        private readonly IStorageManager _storageManager;
        private readonly ILogger<RemittanceService> _logger;
        private readonly RemittanceService _service;

        private const string B2BEnrollType = "B2B";
        private const string C2BEnrollType = "C2B";

        public RemittanceServiceTests()
        {
            _remitApiClient = Mock.Of<IRemittancePartnerNetworkApiClient>(MockBehavior.Strict);
            _remitPartnerNetworkApi = Mock.Of<IRemitPartnerNetworkApi>(MockBehavior.Strict);
            _storageManager = Mock.Of<IStorageManager>(MockBehavior.Strict);
            _logger = new NullLogger<RemittanceService>();
            _service = new RemittanceService(
                _remitApiClient,
                _remitPartnerNetworkApi,
                _storageManager,
                _logger
            );
        }

        public class Constructor : RemittanceServiceTests
        {
            [Fact]
            public void WhenRemittancePartnerNetworkApiClientIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new RemittanceService(
                        null!,
                        _remitPartnerNetworkApi,
                        _storageManager,
                        _logger);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("remittancePartnerNetworkApiClient");
            }

            [Fact]
            public void WhenRemitPartnerNetworkApiIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new RemittanceService(
                        _remitApiClient,
                        null!,
                        _storageManager,
                        _logger);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("remitPartnerNetworkApi");
            }

            [Fact]
            public void WhenStorageManagerIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new RemittanceService(
                        _remitApiClient,
                        _remitPartnerNetworkApi,
                        null!,
                        _logger);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("storageManager");
            }

            [Fact]
            public void WhenLoggerIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new RemittanceService(
                        _remitApiClient,
                        _remitPartnerNetworkApi,
                        _storageManager,
                        null!);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("logger");
            }
        }

        public class PostB2BRemitDocument : RemittanceServiceTests
        {
            [Fact]
            public async Task PostB2BRemitDocument_ReturnsFalseOnEmptyPayload()
            {
                var result = await _service.PostB2BRemitDocument("", CancellationToken.None);

                result.Should().BeFalse();
            }

            [Fact]
            public async Task PostB2BRemitDocument_ReturnsTrueOnAccepted()
            {
                Mock.Get(_remitPartnerNetworkApi)
                    .Setup(r => r.PostB2BRemittanceDocument("payload", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((true, "OK"));

                var result = await _service.PostB2BRemitDocument("payload", CancellationToken.None);

                result.Should().BeTrue();
            }
        }

        public class ValidateB2BRemitDocument : RemittanceServiceTests
        {
            [Fact]
            public async Task ValidateB2BRemitDocument_ReturnsFalseOnEmptyPayload()
            {
                var result = await _service.ValidateB2BRemitDocument("", CancellationToken.None);

                result.Should().BeFalse();
            }

            [Fact]
            public async Task ValidateB2BRemitDocument_ReturnsTrueOnAccepted()
            {
                Mock.Get(_remitPartnerNetworkApi)
                    .Setup(r => r.ValidateB2BRemittanceDocument("payload", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((true, "OK"));

                var result = await _service.ValidateB2BRemitDocument("payload", CancellationToken.None);

                result.Should().BeTrue();
            }
        }

        public class PostRemitDocument : RemittanceServiceTests
        {
            [Fact]
            public async Task PostRemitDocumentForStpPayment_ReturnsFalseOnEmptyPayload()
            {
                var result = await _service.PostRemitDocument(1, B2BEnrollType, "", CancellationToken.None);

                result.Should().BeFalse();
            }

            [Fact]
            public async Task PostRemitDocumentForStpPayment_ReturnsFalseOnInvalidEnrollmentType()
            {
                var result = await _service.PostRemitDocument(1, "INVALID", "payload", CancellationToken.None);

                result.Should().BeFalse();
            }

            [Fact]
            public async Task PostRemitDocumentForStpPayment_ReturnsTrueOnAccepted()
            {
                Mock.Get(_remitApiClient)
                    .Setup(r => r.PostRemittanceDocument(
                        RemittanceNetworkEnrollmentTypes.B2B, "payload", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((true, "OK"));

                var result = await _service.PostRemitDocument(1, B2BEnrollType, "payload", CancellationToken.None);

                result.Should().BeTrue();
            }
        }

        public class ValidateRemitDocument : RemittanceServiceTests
        {
            [Fact]
            public async Task ValidateRemitDocumentForStpPayment_ReturnsFalseOnInvalidEnrollmentType()
            {
                var result = await _service.ValidateRemitDocument(1, "INVALID", "payload", CancellationToken.None);

                result.Should().BeFalse();
            }

            [Fact]
            public async Task ValidateRemitDocumentForStpPayment_ReturnsTrueOnAccepted()
            {
                Mock.Get(_remitApiClient)
                    .Setup(r => r.ValidateRemittanceDocument(
                        RemittanceNetworkEnrollmentTypes.B2B, "payload", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((true, "OK"));

                var result = await _service.ValidateRemitDocument(1, B2BEnrollType, "payload", CancellationToken.None);

                result.Should().BeTrue();
            }
        }
    }
}
