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

public class GetClientBoardingConfigurationTests : IDisposable
{
    private readonly GetClientBoardingConfiguration.Handler _handler;
    private readonly Mock<IMediator> _mediatorMock = new(MockBehavior.Strict);

    public GetClientBoardingConfigurationTests()
    {
        _handler = new GetClientBoardingConfiguration.Handler(_mediatorMock.Object);
    }

    public void Dispose()
    {
        _mediatorMock.VerifyAll();
        GC.SuppressFinalize(this);
    }

    public class Constructor : GetClientBoardingConfigurationTests
    {
        [Fact]
        public void WhenMediatorIsNull_ShouldThrowArgumentNullException()
        {
            new Action(() => _ = new GetClientBoardingConfiguration.Handler(null!))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("mediator");
        }
    }

    public class Handle : GetClientBoardingConfigurationTests
    {
        [Fact]
        public async Task WhenQueryReturnsNull_ShouldThrowValidationException()
        {
            string configurationName = "PREFIX_USERKEY";
            string originCode = "SEMED";
            string clientCode = "ABC";

            var configNotFound = $"Client Boarding Configuration not found for client {clientCode}";
            var configNotFoundEx = new ValidationException(configNotFound);

            _mediatorMock.Setup(x => x.Send(It.Is<ClientBoardingConfigurationQuery>(q => q.ConfigurationName == configurationName && q.OriginCode == originCode && q.ClientCode == clientCode), default))
                .ReturnsAsync((ClientConfigurationDto)null)
                .Verifiable();

            var request = new GetClientBoardingConfiguration.Query(configurationName, originCode, clientCode);

            Func<Task> act = async () => await _handler
                .Handle(request, CancellationToken.None)
                .ConfigureAwait(false);

            await act
                .Should().ThrowAsync<ValidationException>()
                .WithMessage(configNotFound);
        }

        [Fact]
        public async Task WhenQueryThrowsNotFoundException_ShouldThrowValidationException()
        {
            string configurationName = "PREFIX_USERKEY";
            string originCode = "SEMED";
            string clientCode = "ABC";

            var apiException = ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(HttpStatusCode.NotFound), new RefitSettings()).Result;

            var mappingNotFound = $"Client Boarding Configuration not found for client {clientCode}";
            var mappingNotFoundEx = new ValidationException(mappingNotFound);

            _mediatorMock.Setup(x => x.Send(It.Is<ClientBoardingConfigurationQuery>(q => q.ConfigurationName == configurationName && q.OriginCode == originCode && q.ClientCode == clientCode), default))
                .ThrowsAsync(apiException)
                .Verifiable();

            var request = new GetClientBoardingConfiguration.Query(configurationName, originCode, clientCode);

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            await act
                .Should().ThrowAsync<ValidationException>()
                .WithMessage(mappingNotFound);
        }

        [Fact]
        public async Task WhenQueryThrowsInvalidOperationException_ShouldThrowValidationException()
        {
            string configurationName = "PREFIX_USERKEY";
            string originCode = "SEMED";
            string clientCode = "ABC";

            var invalidException = new InvalidOperationException("Something bad happened.");

            var mappingNotFound = $"Client Boarding Configuration is invalid for client {clientCode}";
            var mappingNotFoundEx = new ValidationException(mappingNotFound);

            _mediatorMock.Setup(x => x.Send(It.Is<ClientBoardingConfigurationQuery>(q => q.ConfigurationName == configurationName && q.OriginCode == originCode && q.ClientCode == clientCode), default))
                .ThrowsAsync(invalidException)
                .Verifiable();

            var request = new GetClientBoardingConfiguration.Query(configurationName, originCode, clientCode);

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            await act
                .Should().ThrowAsync<ValidationException>()
                .WithMessage(mappingNotFound);
        }

        [Theory]
        [InlineData("SEMED", "ABC", "INACTIVE", "N")]
        [InlineData("SEMED", "ABC", "INACTIVE", "Y")]
        [InlineData("SEMED", "ABC", "ACTIVE", "N")]
        [InlineData("SEMED", "ABC", "ACTIVE", "Y")]
        public async Task WhenQueryIsValid_ReturnsConfiguration(string originCode, string clientCode, string status, string enabled)
        {
            string configurationName = "PREFIX_USERKEY";

            ClientConfigurationDto clientConfig = new()
            {
                OriginCode = originCode,
                ClientCode = clientCode,
                Status = status,
                Data1 = enabled
            };

            _mediatorMock.Setup(x => x.Send(It.Is<ClientBoardingConfigurationQuery>(q => q.ConfigurationName == configurationName && q.OriginCode == originCode && q.ClientCode == clientCode), default))
                .ReturnsAsync(clientConfig)
                .Verifiable();

            var request = new GetClientBoardingConfiguration.Query(configurationName, originCode, clientCode);

            var result = await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.OriginCode.Should().Be(originCode);
                result.ClientCode.Should().Be(clientCode);
                result.Status.Should().Be(status);
                result.Data1.Should().Be(enabled);
            }
        }
    }
}
