using System.Globalization;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Enrollments;
using VPay.Gateway.Common.Enrollments.Models;
using VPay.Gateway.Common.PlatformConfig;
using Xunit;

namespace VPay.Gateway.Application.Tests;

/// <summary>
/// Tests for <see cref="CreateEnrollmentCommandHandler"/>
/// </summary>
public class CreateEnrollmentCommandHandlerTests : IDisposable
{
    private readonly Mock<IEnrollmentsApi> _enrollmentsApiMock = new Mock<IEnrollmentsApi>(MockBehavior.Strict);
    private readonly Mock<IUserInfo> _userInfoMock = new Mock<IUserInfo>(MockBehavior.Strict);
    private readonly ILogger<CreateEnrollmentCommandHandler> _logger = new NullLogger<CreateEnrollmentCommandHandler>();
    private readonly Mock<IMediator> _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
    private readonly CreateEnrollmentCommandHandler _handler;

    public CreateEnrollmentCommandHandlerTests() =>
        _handler = new CreateEnrollmentCommandHandler(
            _enrollmentsApiMock.Object,
            _logger,
            _mediatorMock.Object,
            _userInfoMock.Object);

    public void Dispose()
    {
        _enrollmentsApiMock.VerifyAll();
        _mediatorMock.VerifyAll();
    }

    public class Constructor : CreateEnrollmentCommandHandlerTests
    {
        [Fact]
        public void WhenEnrollmentsApiIsNull_ThrowsArgumentNullException() =>
            new Action(() => new CreateEnrollmentCommandHandler(null!, _logger, _mediatorMock.Object, _userInfoMock.Object))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("enrollmentsApi");

        [Fact]
        public void WhenLoggerIsNull_ThrowsArgumentNullException() =>
            new Action(() => new CreateEnrollmentCommandHandler(_enrollmentsApiMock.Object, null!, _mediatorMock.Object, _userInfoMock.Object))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("logger");

        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException() =>
            new Action(() => new CreateEnrollmentCommandHandler(_enrollmentsApiMock.Object, _logger, null!, _userInfoMock.Object))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("mediator");
    }

    public class Handle : CreateEnrollmentCommandHandlerTests
    {
        private const string OriginCode = "IC";
        private const string ClientCode = "SWK";
        private const string BillingEntityCode = "SWK0001001";

        [Fact]
        public async Task WhenCreatingEnrollment_ExpectedPropertiesAreSet()
        {
            EnrollmentRequest capturedRequest = null;
            var captureMatch = new CaptureMatch<EnrollmentRequest>(x => capturedRequest = x);

            _userInfoMock
                .Setup(x => x.CanAccess(ClientCode, BillingEntityCode))
                .Returns(true)
                .Verifiable();

            _enrollmentsApiMock
                .Setup(x => x.CreateEnrollment(Capture.With(captureMatch), CancellationToken.None))
                .Returns(Unit.Task)
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.Is<OriginClientQuery>(q => q.ClientCode == ClientCode), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OriginClientDto { ClientCode = ClientCode, Origin = new OriginInfoDto { OriginCode = OriginCode } })
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.Is<EnrollmentConfigurationQuery>(q => q.OriginCode == OriginCode && q.ClientCode == ClientCode), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemberPaymentEnrolledConfigDto { IsEnrolled = true })
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<HasActiveEnrollmentQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false)
                .Verifiable();

            var command = new CreateEnrollmentCommand(
                ClientCode,
                BillingEntityCode,
                "Test Payee",
                "payee_email@test.com",
                "123-456",
                "987-654",
                "555-867-5309");
            await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            capturedRequest.Should().NotBeNull();
            capturedRequest.PayeeList.Should().NotBeEmpty().And.HaveCount(1);
            capturedRequest.PayeeList[0].Should().BeEquivalentTo(
                new PartyEnrollmentRequest
                {
                    Origin = OriginCode,
                    TPA = command.ClientCode,
                    BillingEntity = command.BillingEntityCode,
                    PayeeName = command.PayeeName,
                    PayeeEmail = command.PayeeEmail,
                    Phone1 = command.CellPhoneNumber,
                    Phone1Type = "cell",
                    MemberID = command.MemberId,
                    PolicyNumber = command.PolicyNumber,
                    FullColorLogo = string.Format("/home/bogner/{0}/{0}-full-color-logo.jpg", command.ClientCode.ToLowerInvariant()),
                    TimeoutDate = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"),
                },
                options => options.Excluding(x => x.DateOfFileTimestamp));

            // We should compare this field is in the right format and "close to" the right date
            var dateOfFileTimestamp = DateTime.ParseExact(capturedRequest.PayeeList[0].DateOfFileTimestamp, "yyyy-MM-dd-HH.mm.ss.ffffff", CultureInfo.InvariantCulture);
            dateOfFileTimestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task WhenEnrollmentAlreadyInProgress_ThrowsEnrollmentInProgressException()
        {
            var command = new CreateEnrollmentCommand(
                ClientCode,
                BillingEntityCode,
                "Test Payee",
                "payee_email@test.com",
                "123-456",
                "987-654",
                "555-867-5309");

            _userInfoMock
                .Setup(x => x.CanAccess(ClientCode, BillingEntityCode))
                .Returns(true)
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.Is<OriginClientQuery>(q => q.ClientCode == ClientCode), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OriginClientDto { ClientCode = ClientCode, Origin = new OriginInfoDto { OriginCode = OriginCode } })
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.Is<EnrollmentConfigurationQuery>(q => q.OriginCode == OriginCode && q.ClientCode == ClientCode), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemberPaymentEnrolledConfigDto { IsEnrolled = true })
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.Is<HasActiveEnrollmentQuery>(e => e.ClientCode == command.ClientCode && e.MemberId == command.MemberId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .Verifiable();

            Func<Task> func = async () => await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);
            var exception = await func.Should().ThrowExactlyAsync<EnrollmentInProgressException>();
            exception.And.ClientCode.Should().Be(command.ClientCode);
            exception.And.MemberId.Should().Be(command.MemberId);
        }

        [Fact]
        public async Task WhenEnrollmentNotAllowed_ThrowsUnauthorizedException()
        {
            var command = new CreateEnrollmentCommand(
                ClientCode,
                BillingEntityCode,
                "Test Payee",
                "payee_email@test.com",
                "123-456",
                "987-654",
                "555-867-5309");

            _userInfoMock
                .Setup(x => x.CanAccess(ClientCode, BillingEntityCode))
                .Returns(true)
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.Is<OriginClientQuery>(q => q.ClientCode == ClientCode), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OriginClientDto { ClientCode = ClientCode, Origin = new OriginInfoDto { OriginCode = OriginCode } })
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.Is<EnrollmentConfigurationQuery>(q => q.OriginCode == OriginCode && q.ClientCode == ClientCode), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemberPaymentEnrolledConfigDto { IsEnrolled = false })
                .Verifiable();

            Func<Task> func = async () => await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);
            await func.Should().ThrowExactlyAsync<NotAuthorizedException>().ConfigureAwait(false);
        }
    }
}
