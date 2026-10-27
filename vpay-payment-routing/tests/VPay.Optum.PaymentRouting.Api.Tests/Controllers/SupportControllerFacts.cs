using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Optum.PaymentRouting.Api.Controllers;
using VPay.Optum.PaymentRouting.Api.Controllers.Models;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Commands;
using VPay.Optum.PaymentRouting.ApProcessor.Abstractions;
using VPay.Optum.PaymentRouting.Common.Configuration;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Api.Tests.Controllers
{
    public class SupportControllerFacts
    {
        private static SqlException NewSqlException(string message = null)
        {
            var exception = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SqlException)) as SqlException;

            if (!string.IsNullOrEmpty(message))
            {
                typeof(SqlException)
                    .GetField("_message", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(exception, message);
            }

            return exception;
        }

        private const string EX_NOT_EXISTS = "not exists";
        private const string EX_INVALID_OP = "invalid operation";
        private const long PAYMENTID_NOTEXIST = 1001;
        private const long PAYMENTID_HASOUTBOUNDREFEO = 1002;
        private const long PAYMENTID_NOBATCH = 1003;
        private const long PAYMENTID_BATCHCOMPLETE = 1004;
        private const long PAYMENTID_BATCHINCOMPLETE = 1005;
        private const string USERKEY_NOTMATCH = "11111";
        private const string USERKEY_MATCH = "00000";
        private const string JIRA_VALID = "JIRA-12345";
        private const long BATCHID_NOTEXIST = 1;
        private const long BATCHID_COMPLETE = 2;
        private const long BATCHID_INCOMPLETE = 3;

        private static IMediator MockMediator()
        {
            var mock = new Mock<IMediator>();

            mock.Setup(m => m.Send<UpdateInboundPaymentUserKeySql.Result>(
                    It.Is<UpdateInboundPaymentUserKeySql.Command>(x => x.InboundPaymentId == PAYMENTID_NOTEXIST),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(NewSqlException(EX_NOT_EXISTS));
            mock.Setup(m => m.Send<UpdateInboundPaymentUserKeySql.Result>(
                    It.Is<UpdateInboundPaymentUserKeySql.Command>(x => x.UserKey == USERKEY_NOTMATCH),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(NewSqlException(EX_NOT_EXISTS));
            mock.Setup(m => m.Send<UpdateInboundPaymentUserKeySql.Result>(
                    It.Is<UpdateInboundPaymentUserKeySql.Command>(x => x.InboundPaymentId == PAYMENTID_HASOUTBOUNDREFEO && x.UserKey != USERKEY_NOTMATCH),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(NewSqlException(EX_INVALID_OP));
            mock.Setup(m => m.Send<UpdateInboundPaymentUserKeySql.Result>(
                    It.Is<UpdateInboundPaymentUserKeySql.Command>(x => x.InboundPaymentId == PAYMENTID_NOBATCH && x.UserKey != USERKEY_NOTMATCH),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(NewSqlException(EX_INVALID_OP));
            mock.Setup(m => m.Send<UpdateInboundPaymentUserKeySql.Result>(
                    It.Is<UpdateInboundPaymentUserKeySql.Command>(x => x.InboundPaymentId == PAYMENTID_BATCHCOMPLETE && x.UserKey != USERKEY_NOTMATCH),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(NewSqlException(EX_INVALID_OP));
            mock.Setup(m => m.Send<UpdateInboundPaymentUserKeySql.Result>(
                    It.Is<UpdateInboundPaymentUserKeySql.Command>(x => x.InboundPaymentId == PAYMENTID_BATCHINCOMPLETE && x.UserKey != USERKEY_NOTMATCH),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateInboundPaymentUserKeySql.Result(USERKEY_MATCH + "_" + JIRA_VALID));

            mock.Setup(m => m.Send(
                    It.Is<CancelInboundPayment.Command>(x => x.InboundPaymentId == PAYMENTID_NOTEXIST),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(EX_NOT_EXISTS));
            mock.Setup(m => m.Send(
                    It.Is<CancelInboundPayment.Command>(x => x.InboundPaymentId == PAYMENTID_NOBATCH),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(EX_NOT_EXISTS));
            mock.Setup(m => m.Send(
                    It.Is<CancelInboundPayment.Command>(x => x.InboundPaymentId == PAYMENTID_BATCHCOMPLETE),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(EX_INVALID_OP));
            mock.Setup(m => m.Send(
                    It.Is<CancelInboundPayment.Command>(x => x.InboundPaymentId == PAYMENTID_BATCHINCOMPLETE),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            mock.Setup(m => m.Send(
                    It.Is<CancelInboundBatch.Command>(x => x.InboundBatchId == BATCHID_NOTEXIST),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(EX_NOT_EXISTS));
            mock.Setup(m => m.Send(
                    It.Is<CancelInboundBatch.Command>(x => x.InboundBatchId == BATCHID_COMPLETE),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(EX_INVALID_OP));
            mock.Setup(m => m.Send(
                    It.Is<CancelInboundBatch.Command>(x => x.InboundBatchId == BATCHID_INCOMPLETE),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            mock.Setup(m => m.Send(
                    It.Is<UpdateInboundBatchTotalsSql.Command>(x => x.InboundBatchId == BATCHID_NOTEXIST),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(NewSqlException(EX_NOT_EXISTS));
            mock.Setup(m => m.Send(
                    It.Is<UpdateInboundBatchTotalsSql.Command>(x => x.InboundBatchId == BATCHID_COMPLETE),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(NewSqlException(EX_INVALID_OP));
            mock.Setup(m => m.Send(
                    It.Is<UpdateInboundBatchTotalsSql.Command>(x => x.InboundBatchId == BATCHID_INCOMPLETE),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            mock.Setup(m => m.Send(
             It.Is<GetIncompleteBatchesSql.Query>(x => (x.MinutesLag.HasValue && x.MinutesLag > 10)),
             It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IncompleteBatchDetails> { new IncompleteBatchDetails() });

            mock.Setup(m => m.Send(
                    It.Is<SaveBatchSummary.Command>(x => x.PaymentSummary.BatchId == BATCHID_NOTEXIST.ToString()),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary());
            mock.Setup(m => m.Send(
                    It.Is<SaveBatchSummary.Command>(x => x.PaymentSummary.BatchId == BATCHID_COMPLETE.ToString()),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary());
            mock.Setup(m => m.Send(
                    It.Is<SaveBatchSummary.Command>(x => x.PaymentSummary.BatchId == BATCHID_INCOMPLETE.ToString()),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary());

            mock.Setup(m => m.Send(
                    It.Is<PublishPaymentSummary.Command>(x => x.PaymentSummary.BatchId == BATCHID_NOTEXIST.ToString()),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);
            mock.Setup(m => m.Send(
                    It.Is<PublishPaymentSummary.Command>(x => x.PaymentSummary.BatchId == BATCHID_COMPLETE.ToString()),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);
            mock.Setup(m => m.Send(
                    It.Is<PublishPaymentSummary.Command>(x => x.PaymentSummary.BatchId == BATCHID_INCOMPLETE.ToString()),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            mock.Setup(m => m.Send(
                    It.Is<SavePaymentRecord.Command>(x => x.PaymentRecord.BatchId == BATCHID_NOTEXIST.ToString()),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary());
            mock.Setup(m => m.Send(
                    It.Is<SavePaymentRecord.Command>(x => x.PaymentRecord.BatchId == BATCHID_COMPLETE.ToString()),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary());
            mock.Setup(m => m.Send(
                    It.Is<SavePaymentRecord.Command>(x => x.PaymentRecord.BatchId == BATCHID_INCOMPLETE.ToString()),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary());

            mock.Setup(m => m.Send(
                    It.Is<CheckBatchCompletion.Command>(x => x == new CheckBatchCompletion.Command(BATCHID_NOTEXIST)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);
            mock.Setup(m => m.Send(
                    It.Is<CheckBatchCompletion.Command>(x => x == new CheckBatchCompletion.Command(BATCHID_COMPLETE)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);
            mock.Setup(m => m.Send(
                     It.Is<CheckBatchCompletion.Command>(x => x == new CheckBatchCompletion.Command(BATCHID_INCOMPLETE)),
                     It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Unit.Value);

            return mock.Object;
        }

        private readonly PaymentRoutingTestingContainer _container;
        private readonly OptumQueueNameSettings _settings = new() { AccountPayable = "test-ap" };

        public SupportControllerFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services
                    .AddSingleton<SupportController>()
                    .AddSingleton(MockMediator())
                    .AddMock<IOptumBus>()
                    .AddMock<IFileSystem>()
                    .AddMock<IUserInfo>()
                    .AddMock<IApReapVPayToOptumConverter>()
                    .AddSingleton(_ => _settings)
                    .AddLogging(_ => _.AddProvider(NullLoggerProvider.Instance));
            });

        [Theory]
        [InlineData(BATCHID_INCOMPLETE, 1, 1d)]
        public async Task Should_Returns200_When_UpdateBatchTotals(long id, int count, decimal amount)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            async Task<IActionResult> act() => await controller.UpdateBatchTotals(
                id, count, amount, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<OkResult>();
        }

        [Theory]
        [InlineData(BATCHID_COMPLETE, 1, 1d)]
        public async Task Should_Returns400_When_UpdateBatchTotals(long id, int count, decimal amount)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            async Task<IActionResult> act() => await controller.UpdateBatchTotals(
                id, count, amount, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<BadRequestObjectResult>();
        }

        [Theory]
        [InlineData(BATCHID_NOTEXIST, 1, 1d)]
        public async Task Should_Returns404_When_UpdateBatchTotals(long id, int count, decimal amount)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            async Task<IActionResult> act() => await controller.UpdateBatchTotals(
                id, count, amount, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<NotFoundObjectResult>();
        }

        [Theory]
        [InlineData(PAYMENTID_BATCHINCOMPLETE, USERKEY_MATCH, JIRA_VALID)]
        public async Task Should_Returns200_When_UpdateUserKey(long id, string userKey, string jiraIssueId)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IActionResult>> act = async () => await controller.UpdateUserKey(
                id, userKey, jiraIssueId, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<OkObjectResult>();
        }

        [Theory]
        [InlineData(PAYMENTID_BATCHCOMPLETE, USERKEY_MATCH, JIRA_VALID)]
        [InlineData(PAYMENTID_HASOUTBOUNDREFEO, USERKEY_MATCH, JIRA_VALID)]
        [InlineData(PAYMENTID_NOBATCH, USERKEY_MATCH, JIRA_VALID)]
        public async Task Should_Returns400_When_UpdateUserKey(long id, string userKey, string jiraIssueId)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IActionResult>> act = async () => await controller.UpdateUserKey(
                id, userKey, jiraIssueId, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<BadRequestObjectResult>();
        }

        [Theory]
        [InlineData(PAYMENTID_NOTEXIST, USERKEY_MATCH, JIRA_VALID)]
        [InlineData(PAYMENTID_BATCHINCOMPLETE, USERKEY_NOTMATCH, JIRA_VALID)]
        public async Task Should_Returns404_When_UpdateUserKey(long id, string userKey, string jiraIssueId)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IActionResult>> act = async () => await controller.UpdateUserKey(
                id, userKey, jiraIssueId, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<NotFoundObjectResult>();
        }

        [Theory]
        [InlineData(PAYMENTID_BATCHINCOMPLETE)]
        public async Task Should_Returns200_When_CancelPayment(long id)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IActionResult>> act = async () => await controller.CancelPayment(
                id, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<OkResult>();
        }

        [Theory]
        [InlineData(PAYMENTID_BATCHCOMPLETE)]
        public async Task Should_Returns400_When_CancelPayment(long id)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IActionResult>> act = async () => await controller.CancelPayment(
                id, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<BadRequestObjectResult>();
        }

        [Theory]
        [InlineData(PAYMENTID_NOTEXIST)]
        [InlineData(PAYMENTID_NOBATCH)]
        public async Task Should_Returns404_When_CancelPayment(long id)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IActionResult>> act = async () => await controller.CancelPayment(
                id, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<NotFoundObjectResult>();
        }

        [Theory]
        [InlineData(BATCHID_INCOMPLETE)]
        public async Task Should_Returns200_When_CancelBatch(long id)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IActionResult>> act = async () => await controller.CancelBatch(
                id, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<OkResult>();
        }

        [Theory]
        [InlineData(BATCHID_COMPLETE)]
        public async Task Should_Returns400_When_CancelBatch(long id)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IActionResult>> act = async () => await controller.CancelBatch(
                id, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<BadRequestObjectResult>();
        }

        [Theory]
        [InlineData(BATCHID_NOTEXIST)]
        public async Task Should_Returns404_When_CancelBatch(long id)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IActionResult>> act = async () => await controller.CancelBatch(
                id, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<NotFoundObjectResult>();
        }

        [Theory]
        [InlineData(BATCHID_NOTEXIST)]
        [InlineData(BATCHID_COMPLETE)]
        [InlineData(BATCHID_INCOMPLETE)]
        public async Task QueuePaymentRecord_Should_Return_200OK(long id)
        {
            // arrange
            var controller = _container.Get<SupportController>();
            var request = new QueuePaymentRecordBody
            {
                PaymentRecord = new PaymentRecord
                {
                    BatchId = id.ToString()
                }
            };

            // act
            Func<Task<IActionResult>> act = async () => await controller.QueuePaymentRecord(
                request, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<OkResult>();
        }

        [Theory]
        [InlineData(BATCHID_NOTEXIST)]
        [InlineData(BATCHID_COMPLETE)]
        [InlineData(BATCHID_INCOMPLETE)]
        public async Task PublishPaymentSummary_Should_Return_200OK(long id)
        {
            // arrange
            var controller = _container.Get<SupportController>();
            var request = new PaymentSummary
            {
                BatchId = id.ToString()
            };

            // act
            Func<Task<Unit>> act = async () => await controller.PublishPaymentSummary(
                request, CancellationToken.None);

            // assert
            (await act()).Should().BeOfType<Unit>().And.Be(Unit.Value);
        }

        [Theory]
        [InlineData(BATCHID_NOTEXIST)]
        [InlineData(BATCHID_COMPLETE)]
        [InlineData(BATCHID_INCOMPLETE)]
        public async Task SavePaymentSummary_Should_Return_200OK(long id)
        {
            // arrange
            var controller = _container.Get<SupportController>();
            var request = new PaymentSummary
            {
                BatchId = id.ToString()
            };

            // act
            Func<Task<ActionResult<BatchSummary>>> act = async () => await controller.SavePaymentSummary(
                request, CancellationToken.None);

            // assert
            (await act()).Result.Should().BeOfType<OkObjectResult>();
        }

        [Theory]
        [InlineData(BATCHID_NOTEXIST)]
        [InlineData(BATCHID_COMPLETE)]
        [InlineData(BATCHID_INCOMPLETE)]
        public async Task SavePaymentRecord_Should_Return_200OK(long id)
        {
            // arrange
            var controller = _container.Get<SupportController>();
            var request = new QueuePaymentRecordBody
            {
                PaymentRecord = new PaymentRecord
                {
                    BatchId = id.ToString()
                }
            };

            // act
            Func<Task<ActionResult<BatchSummary>>> act = async () => await controller.SavePaymentRecord(
                request, CancellationToken.None);

            // assert
            (await act()).Result.Should().BeOfType<OkObjectResult>();
        }

        [Theory]
        [InlineData(BATCHID_NOTEXIST)]
        [InlineData(BATCHID_COMPLETE)]
        [InlineData(BATCHID_INCOMPLETE)]
        public async Task CheckBatchCompletion_Should_Return_200OK(long id)
        {
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<ActionResult<BatchSummary>>> act = async () => await controller.CheckBatchCompletion(
                id, CancellationToken.None);

            // assert
            (await act()).Result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task Should_Return_Correct_Response_When_GetIncompleteBatchesDetails_Called_With_Null()
        {
            int? id = null;
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IEnumerable<IncompleteBatchDetails>>> act = async () => await controller.GetIncompleteBatchesDetails(
                id, CancellationToken.None);

            // assert
            (await act()).Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task Should_Return_Correct_Response_When_GetIncompleteBatchesDetails_Called_With_LowValue()
        {
            int? id = 10;// Values < 11 will give no results
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IEnumerable<IncompleteBatchDetails>>> act = async () => await controller.GetIncompleteBatchesDetails(
                id, CancellationToken.None);

            // assert
            (await act()).Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task Should_Return_Correct_Response_When_GetIncompleteBatchesDetails_Called_With_Value()
        {
            int? id = 11; // Values > 10
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<IEnumerable<IncompleteBatchDetails>>> act = async () => await controller.GetIncompleteBatchesDetails(
                id, CancellationToken.None);

            // assert
            (await act()).Should().NotBeNullOrEmpty();
            (await act()).Should().HaveCount(1);
        }

        [Fact]
        public async Task Should_Return_Correct_Response_When_GetIncompleteBatchesCount_Called_With_Null()
        {
            int? id = null;
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<int>> act = async () => await controller.GetIncompleteBatchesCount(
                id, CancellationToken.None);

            // assert

            (await act()).Should().Be(0);
        }

        [Fact]
        public async Task Should_Return_Correct_Response_When_GetIncompleteBatchesCount_Called_With_LowValue()
        {
            int? id = 10;// Values < 11 will give no results
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<int>> act = async () => await controller.GetIncompleteBatchesCount(
                id, CancellationToken.None);

            // assert
            (await act()).Should().Be(0);
        }

        [Fact]
        public async Task Should_Return_Correct_Response_When_GetIncompleteBatchesCount_Called_With_Value()
        {
            int? id = 11; // Values > 10
            // arrange
            var controller = _container.Get<SupportController>();

            // act
            Func<Task<int>> act = async () => await controller.GetIncompleteBatchesCount(
                id, CancellationToken.None);

            // assert
            (await act()).Should().Be(1);
        }
    }
}
