using System.Net;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentValidation;
using MediatR;
using Moq;
using Refit;
using VPay.Gateway.Common.PlatformConfig;
using Xunit;

namespace VPay.Gateway.Application.Tests;

public class GetOptumPrsConfigurationTests : IDisposable
{
    private readonly GetOptumPrsConfiguration.Handler _handler;
    private readonly Mock<IMediator> _mediatorMock = new(MockBehavior.Strict);

    public GetOptumPrsConfigurationTests()
    {
        _handler = new GetOptumPrsConfiguration.Handler(_mediatorMock.Object);
    }

    public void Dispose()
    {
        _mediatorMock.VerifyAll();
        GC.SuppressFinalize(this);
    }

    public class Constructor : GetOptumPrsConfigurationTests
    {
        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException()
        {
            new Action(() => _ = new GetOptumPrsConfiguration.Handler(null!))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("mediator");
        }
    }

    public class Handle : GetOptumPrsConfigurationTests
    {
        [Fact]
        public async Task WhenPrsMappingReturnsNull_ShouldThrowException()
        {
            string payerId = "USHA1";
            string subPayerId = "000020943";

            var mappingNotFound = $"Optum PRS Mapping not found for sub-payer {subPayerId}";
            var mappingNotFoundEx = new ValidationException(mappingNotFound);

            _mediatorMock.Setup(x => x.Send(It.Is<OptumSubPayerQuery>(q => q.OptumSubPayerId == subPayerId), default))
                .ReturnsAsync((OptumSubPayerDto)null)
                .Verifiable();

            var request = new GetOptumPrsConfiguration.Query(payerId, subPayerId);

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            await act
                .Should().ThrowAsync<ValidationException>()
                .WithMessage(mappingNotFound);
        }

        [Fact]
        public async Task WhenPrsConfigurationReturnsNull_ShouldThrowException()
        {
            string originCode = "SEMED";
            string clientCode = "USH";
            string beCode = "USH0001001";
            string payerId = "USHA1";
            string subPayerId = "000020943";

            var configNotFound = $"Optum PRS Configuration not found for payer {payerId} and sub-payer {subPayerId}";
            var configNotFoundEx = new ValidationException(configNotFound);

            OptumSubPayerDto subPayerDto = new()
            {
                OptumPayorId = payerId,
                OptumSubPayorId = subPayerId,
                ClientCode = clientCode,
                BillingEntityCode = beCode,
            };

            OriginClientDto originClient = new()
            {
                Origin = new OriginInfoDto { OriginId = 1, OriginCode = originCode },
                ClientCode = clientCode,
                BillingEntityCode = beCode,
            };

            _mediatorMock.Setup(x => x.Send(It.Is<OptumSubPayerQuery>(q => q.OptumSubPayerId == subPayerId), default))
                .ReturnsAsync(subPayerDto)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<OriginClientQuery>(q => q.ClientCode == clientCode), default))
                .ReturnsAsync(originClient)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<OptumPrsConfigurationQuery>(q => q.ClientCode == clientCode && q.BillingEntityCode == beCode), default))
                .ReturnsAsync((OptumPrsConfigurationDto)null)
                .Verifiable();

            var request = new GetOptumPrsConfiguration.Query(payerId, subPayerId);

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            await act
                .Should().ThrowAsync<ValidationException>()
                .WithMessage(configNotFound);
        }

        [Fact]
        public async Task WhenPrsMappingThrowsNotFound_ShouldThrowException()
        {
            string payerId = "USHA1";
            string subPayerId = "000020943";

            var apiException = ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(HttpStatusCode.NotFound), new RefitSettings()).Result;

            var mappingNotFound = $"Optum PRS Mapping not found for sub-payer {subPayerId}";
            var mappingNotFoundEx = new ValidationException(mappingNotFound);

            _mediatorMock.Setup(x => x.Send(It.Is<OptumSubPayerQuery>(q => q.OptumSubPayerId == subPayerId), default))
                .ThrowsAsync(apiException)
                .Verifiable();

            var request = new GetOptumPrsConfiguration.Query(payerId, subPayerId);

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            await act
                .Should().ThrowAsync<ValidationException>()
                .WithMessage(mappingNotFound);
        }

        [Fact]
        public async Task WhenPrsConfigurationThrowsNotFound_ShouldThrowException()
        {
            string originCode = "SEMED";
            string clientCode = "USH";
            string beCode = "USH0001001";
            string payerId = "USHA1";
            string subPayerId = "000020943";

            var apiException = ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(HttpStatusCode.NotFound), new RefitSettings()).Result;

            var configNotFound = $"Optum PRS Configuration not found for payer {payerId} and sub-payer {subPayerId}";
            var configNotFoundEx = new ValidationException(configNotFound);

            OptumSubPayerDto subPayerDto = new()
            {
                OptumPayorId = payerId,
                OptumSubPayorId = subPayerId,
                ClientCode = clientCode,
                BillingEntityCode = beCode,
            };

            OriginClientDto originClient = new()
            {
                Origin = new OriginInfoDto { OriginId = 1, OriginCode = originCode },
                ClientCode = clientCode,
                BillingEntityCode = beCode,
            };

            _mediatorMock.Setup(x => x.Send(It.Is<OptumSubPayerQuery>(q => q.OptumSubPayerId == subPayerId), default))
                .ReturnsAsync(subPayerDto)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<OriginClientQuery>(q => q.ClientCode == clientCode), default))
                .ReturnsAsync(originClient)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<OptumPrsConfigurationQuery>(q => q.ClientCode == clientCode && q.BillingEntityCode == beCode), default))
                .ThrowsAsync(apiException)
                .Verifiable();

            var request = new GetOptumPrsConfiguration.Query(payerId, subPayerId);

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            await act
                .Should().ThrowAsync<ValidationException>()
                .WithMessage(configNotFound);
        }

        [Theory]
        [InlineData(false, "ADN", "ADN0001001", "VPAY5", "01", "VPAY5")]
        [InlineData(false, "NOV", "NOV0001001", "VPAY5", "02", "VHSA5")]
        [InlineData(false, "USH", "USH0001001", "USHA1", "000020943", "VPUSH")]
        [InlineData(true, "UMR", "UMRPF01001", "UMR01", "886000028", "OUT835")]
        [InlineData(true, "HSO", "HSOPF01001", "40026", "381490038", "UMHS5")]
        [InlineData(true, "MHP", "MHP0001001", "MCLRN", "272204037", "MCLN5")]
        public async Task WhenRequestIsValid_ReturnsPrsConfiguration(bool isPrsEnabled, string clientCode, string beCode, string payerId, string subPayerId, string clientGroup)
        {
            string originCode = "SEMED";

            OptumSubPayerDto subPayerDto = new()
            {
                OptumPayorId = payerId,
                OptumSubPayorId = subPayerId,
                ClientCode = clientCode,
                BillingEntityCode = beCode,
            };

            OriginClientDto originClient = new()
            {
                Origin = new OriginInfoDto { OriginId = 1, OriginCode = originCode },
                ClientCode = clientCode,
                BillingEntityCode = beCode,
            };

            OptumPrsConfigurationDto prsConfigDto = new()
            {
                OriginCode = originCode,
                ClientCode = clientCode,
                BillingEntityCode = beCode,
                PrfClientGroup = clientGroup,
                IsPrsEnabled = isPrsEnabled,
            };

            _mediatorMock.Setup(x => x.Send(It.Is<OptumSubPayerQuery>(q => q.OptumSubPayerId == subPayerId), default))
                .ReturnsAsync(subPayerDto)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<OriginClientQuery>(q => q.ClientCode == clientCode), default))
                .ReturnsAsync(originClient)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<OptumPrsConfigurationQuery>(q => q.ClientCode == clientCode && q.BillingEntityCode == beCode), default))
                .ReturnsAsync(prsConfigDto)
                .Verifiable();

            var request = new GetOptumPrsConfiguration.Query(payerId, subPayerId);

            var result = await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.OriginCode.Should().Be(originCode);
                result.ClientCode.Should().Be(clientCode);
                result.BillingEntityCode.Should().Be(beCode);
                result.IsPrsEnabled.Should().Be(isPrsEnabled);
                result.PrfClientGroup.Should().Be(clientGroup);
            }
        }
    }
}
