using System;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using VPay.Extensions.Exceptions;
using VPay.Optum.Http.PreferencingApi.Models;
using VPay.Optum.Http.PreferencingApi.Queries;
using VPay.Optum.PaymentRouting.ApProcessor.Abstractions;
using VPay.Optum.PaymentRouting.ApProcessor.Constants;
using VPay.Optum.PaymentRouting.ApProcessor.Consumers;
using VPay.Optum.PaymentRouting.Common.Configuration;
using VPay.Optum.PaymentRouting.Common.Constants;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using Xunit;

namespace VPay.Optum.PaymentRouting.ApProcessor.Tests.Consumers;

public class ConsumeApMessageFacts
{
    private readonly ILogger<ConsumeApMessage.Handler> _logger = Substitute.For<ILogger<ConsumeApMessage.Handler>>();
    private readonly IApReapVPayToOptumConverter _converter = Substitute.For<IApReapVPayToOptumConverter>();
    private readonly IOptumBus _bus = Substitute.For<IOptumBus>();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IUserInfo _userInfo = Substitute.For<IUserInfo>();
    private readonly OptumQueueNameSettings _settings = new() { AccountPayable = "AP_Queue" };

    private ConsumeApMessage.Handler CreateHandler()
    {
        _userInfo.GetCurrentUserName().Returns("Test_User");
        return new ConsumeApMessage.Handler(_logger, _converter, _bus, _publishEndpoint, _mediator, _userInfo, _settings);
    }

    private static ApReapMessage CreateMessage(string paymentType, string messageType)
    {
        return new ApReapMessage
        {
            PaymentType = paymentType,
            MessageType = messageType,
            TpaCode = "TPA",
            BillingEntity = "BE",
            ProviderTin = "TIN",
            UserKey = "UK"
        };
    }

    private static VpayApReap CreateConvertedMessage()
    {
        return new VpayApReap();
    }

    private static OptumAchEnrollment CreateOptumEnrollment(bool isEnrolled)
    {
        return new OptumAchEnrollment(ClientCode: "TPA", TaxId: "TIN", IsEnrolled: isEnrolled);
    }

    [Fact]
    public async Task ProcessApMessage_Routes_OptumAch_Payment_Correctly()
    {
        var handler = CreateHandler();
        var message = CreateMessage(PaymentTypes.Opach, MessageTypes.Ap);
        var converted = CreateConvertedMessage();
        var enrollment = CreateOptumEnrollment(isEnrolled: false);

        _mediator.Send(Arg.Any<OptumAchEnrollmentSearchQuery>(), Arg.Any<CancellationToken>()).Returns(enrollment); // IsOptumEnrolledNoPay

        await handler.ProcessApMessage(1, 2, message, converted, CancellationToken.None);

        await _publishEndpoint.Received().Publish(Arg.Is<ApReapTracking>(t =>
            t.ApRoutingType == RoutingTypes.Opach &&
            t.ApMessageTrackingStatus == ApMessageTrackingStatus.Completed.Name &&
            t.InboundPaymentStatus == PaymentStatus.ApSentSkip.Name
        ), CancellationToken.None);
    }

    [Fact]
    public async Task ProcessApMessage_Routes_ClientEft_Payment_Correctly()
    {
        var handler = CreateHandler();
        var message = CreateMessage(PaymentTypes.Clcft, MessageTypes.Ap);
        var converted = CreateConvertedMessage();
        var enrollment = CreateOptumEnrollment(isEnrolled: false);

        _mediator.Send(Arg.Any<OptumAchEnrollmentSearchQuery>(), Arg.Any<CancellationToken>()).Returns(enrollment); // IsOptumEnrolledNoPay

        await handler.ProcessApMessage(1, 2, message, converted, CancellationToken.None);

        await _publishEndpoint.Received().Publish(Arg.Is<ApReapTracking>(t =>
            t.ApRoutingType == RoutingTypes.FinSeg &&
            t.ApMessageTrackingStatus == ApMessageTrackingStatus.FinSegNeeded.Name &&
            t.InboundPaymentStatus == PaymentStatus.FinSegQueued.Name
        ), CancellationToken.None);
    }

    [Fact]
    public async Task ProcessApMessage_Routes_Enrolled_NoPay_Payment_Correctly()
    {
        var handler = CreateHandler();
        var message = CreateMessage(PaymentTypes.Clnpf, MessageTypes.Ap);
        var converted = CreateConvertedMessage();
        var enrollment = CreateOptumEnrollment(isEnrolled: true);

        _mediator.Send(Arg.Any<OptumAchEnrollmentSearchQuery>(), Arg.Any<CancellationToken>()).Returns(enrollment); // IsOptumEnrolledNoPay

        await handler.ProcessApMessage(1, 2, message, converted, CancellationToken.None);

        await _publishEndpoint.Received().Publish(Arg.Is<ApReapTracking>(t =>
            t.ApRoutingType == RoutingTypes.FinSeg &&
            t.ApMessageTrackingStatus == ApMessageTrackingStatus.FinSegNeeded.Name &&
            t.InboundPaymentStatus == PaymentStatus.FinSegQueued.Name &&
            t.IsOptumEnrolled == true
        ), CancellationToken.None);
    }

    [Fact]
    public async Task ProcessApMessage_Routes_Azure_AP_Payment_Correctly()
    {
        var handler = CreateHandler();
        var message = CreateMessage("OtherType", MessageTypes.Ap);
        var converted = CreateConvertedMessage();
        var enrollment = CreateOptumEnrollment(isEnrolled: false);

        _mediator.Send(Arg.Any<OptumAchEnrollmentSearchQuery>(), Arg.Any<CancellationToken>()).Returns(enrollment); // IsOptumEnrolledNoPay

        await handler.ProcessApMessage(1, 2, message, converted, CancellationToken.None);

        await _publishEndpoint.Received().Publish(Arg.Is<ApReapTracking>(t =>
            t.ApRoutingType == RoutingTypes.Azure &&
            t.ApMessageTrackingStatus == ApMessageTrackingStatus.ApSent.Name &&
            t.InboundPaymentStatus == PaymentStatus.ApSent.Name
        ), CancellationToken.None);
    }

    [Fact]
    public async Task ProcessApMessage_Routes_Azure_REAP_Payment_Correctly()
    {
        var handler = CreateHandler();
        var message = CreateMessage("OtherType", MessageTypes.Reap);
        var converted = CreateConvertedMessage();
        var enrollment = CreateOptumEnrollment(isEnrolled: false);

        _mediator.Send(Arg.Any<OptumAchEnrollmentSearchQuery>(), Arg.Any<CancellationToken>()).Returns(enrollment); // IsOptumEnrolledNoPay

        await handler.ProcessApMessage(1, 2, message, converted, CancellationToken.None);

        await _mediator.Received().Send(Arg.Is<UpdateApMessageTrackingStatus.Command>(c =>
            c.ApMessageTrackingId == 1 &&
            c.Status == ApMessageTrackingStatus.Completed
        ), CancellationToken.None);
    }

    [Fact]
    public async Task ProcessApMessage_Throws_On_Exception()
    {
        var handler = CreateHandler();
        var message = CreateMessage(PaymentTypes.Opach, MessageTypes.Ap);
        var converted = CreateConvertedMessage();

        _publishEndpoint.When(x => x.Publish(Arg.Any<ApReapTracking>(), Arg.Any<CancellationToken>()))
            .Do(x => throw new InvalidOperationException("Test error"));

        Func<Task> act = async () => await handler.ProcessApMessage(1, 2, message, converted, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetApMessageTrackingId_ReturnsSuccess_WhenQueued()
    {
        var handler = CreateHandler();
        var message = new ApReapMessage { TransactionId = 123, UserKey = "UK", TrnSegment = "TS", LoadAmount = 10, CheckNumber = "CHK", LoadDate = DateTime.Today, PaymentType = "PT" };
        var tracking = new ApMessageTracking { ApMessageTrackingId = 1, Status = ApMessageTrackingStatus.Queued };
        _mediator.Send(Arg.Any<GetApMessageTrackingByApMessageId.Query>(), Arg.Any<CancellationToken>()).Returns(tracking);

        var result = await handler.GetApMessageTrackingId(message, 99L, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(tracking.ApMessageTrackingId);
    }

    [Fact]
    public async Task GetApMessageTrackingId_ThrowsConflictException_WhenInsertFails()
    {
        var handler = CreateHandler();
        var message = new ApReapMessage { TransactionId = 123, UserKey = "UK", TrnSegment = "TS", LoadAmount = 10, CheckNumber = "CHK", LoadDate = DateTime.Today, PaymentType = "PT" };
        _mediator.Send(Arg.Any<GetApMessageTrackingByApMessageId.Query>(), Arg.Any<CancellationToken>()).ReturnsNull();
        _mediator.Send(Arg.Any<InsertApMessageTracking.Command>(), Arg.Any<CancellationToken>()).Returns(Result<long>.Error());

        Func<Task> act = async () => await handler.GetApMessageTrackingId(message, 99L, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public void GetWorkingMessage_ReturnsReapMessage_WhenMessageTypeIsReap()
    {
        var handler = CreateHandler();
        var reapDate = DateTime.Today.AddDays(-1);
        var message = new ApReapMessage
        {
            MessageType = MessageTypes.Reap,
            TransactionId = 1,
            ReapTransactionId = 2,
            ReapCheckNumber = "RCN",
            ReapLoadDate = reapDate,
            ReapPaymentType = "RPT"
        };

        var result = handler.GetWorkingMessage(message);

        result.TransactionId.Should().Be(message.ReapTransactionId);
        result.CheckNumber.Should().Be(message.ReapCheckNumber);
        result.LoadDate.Should().Be(reapDate);
        result.PaymentType.Should().Be(message.ReapPaymentType);
    }

    [Fact]
    public void GetWorkingMessage_ReturnsOriginalMessage_WhenMessageTypeIsAp()
    {
        var handler = CreateHandler();
        var message = new ApReapMessage { MessageType = MessageTypes.Ap, TransactionId = 1 };
        var result = handler.GetWorkingMessage(message);

        result.Should().Be(message);
    }

    [Fact]
    public async Task IsOptumEnrolledNoPay_ReturnsFalse_WhenNotNoPay()
    {
        var handler = CreateHandler();
        var message = new ApReapMessage { PaymentType = "NotNoPay" };
        var result = await handler.IsOptumEnrolledNoPay(message, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsOptumEnrolledNoPay_ReturnsTrue_WhenNoPayAndEnrolled()
    {
        var handler = CreateHandler();
        var message = new ApReapMessage { PaymentType = PaymentTypes.Clnpf, TpaCode = "TPA", ProviderTin = "TIN" };
        _mediator
            .Send(Arg.Any<OptumAchEnrollmentSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new OptumAchEnrollment(ClientCode: "TPA", TaxId: "TIN", IsEnrolled: true));

        var result = await handler.IsOptumEnrolledNoPay(message, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetInbound835Payment_ThrowsItemNotFoundException_WhenNull()
    {
        var handler = CreateHandler();
        var message = new ApReapMessage { UserKey = "UK" };
        var converted = new VpayApReap { payr835ID = "PID" };
        _mediator.Send(Arg.Any<GetInboundPaymentByUserKeyPayer835IdSql.Query>(), Arg.Any<CancellationToken>()).ReturnsNull();

        Func<Task> act = async () => await handler.GetInbound835Payment(message, converted, CancellationToken.None);

        await act.Should().ThrowAsync<ItemNotFoundException>();
    }

    [Fact]
    public async Task SendApInfoToOptumViaAzureBus_SendsMessage()
    {
        var handler = CreateHandler();
        var message = new ApReapMessage { MessageType = MessageTypes.Ap, UserKey = "UK" };
        var converted = new VpayApReap();
        var endpoint = Substitute.For<ISendEndpoint>();
        _bus.GetSendEndpoint(Arg.Any<Uri>()).Returns(endpoint);

        await handler.SendApInfoToOptumViaAzureBus(message, converted, CancellationToken.None);

        await endpoint.Received().Send(converted, CancellationToken.None);
    }

    [Fact]
    public async Task UpdateTrackingStatus_CallsMediator()
    {
        var handler = CreateHandler();
        await handler.UpdateTrackingStatus(1L, ApMessageTrackingStatus.Completed, CancellationToken.None);

        await _mediator.Received().Send(Arg.Is<UpdateApMessageTrackingStatus.Command>(x => x.ApMessageTrackingId == 1L && x.Status == ApMessageTrackingStatus.Completed), CancellationToken.None);
    }

    [Fact]
    public async Task UpdatePaymentStatus_CallsMediator()
    {
        var handler = CreateHandler();
        await handler.UpdatePaymentStatus(2L, PaymentStatus.ApSent, CancellationToken.None);

        await _mediator.Received().Send(Arg.Is<UpdateInboundPaymentStatusSql.Command>(x => x.InboundPaymentId == 2L && x.Status == PaymentStatus.ApSent), CancellationToken.None);
    }

    [Theory]
    [InlineData("sub_123", "sub", "123")]
    [InlineData("123", "sub", "123")]
    public void StripSubPayerIdIfExists_ReturnsExpected(string userKey, string subPayerId, string expected)
    {
        var handler = CreateHandler();
        var result = ConsumeApMessage.Handler.StripSubPayerIdIfExists(userKey, subPayerId);

        result.Should().Be(expected);
    }
}
