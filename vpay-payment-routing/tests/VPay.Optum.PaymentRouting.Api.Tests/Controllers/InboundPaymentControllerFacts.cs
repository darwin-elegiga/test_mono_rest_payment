using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VPay.Optum.PaymentRouting.Api.Controllers;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using Xunit;

namespace VPay.Optum.PaymentRouting.Api.Tests.Controllers
{
    public class InboundPaymentControllerFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        private const long PAYERID_VALID = 1;
        private const long PAYMENTID_VALID = 1;
        private const long PAYERID_INVALID = 0;
        private const long PAYMENTID_INVALID = 0;

        private static IMediator MockMediator()
        {
            var mock = new Mock<IMediator>();

            // GetPaymentBatch
            mock.Setup(x => x.Send(
                    It.Is<GetPaymentByPayerIdUserKey.Query>(
                        x => x.PayerId.Equals(PAYERID_VALID.ToString())
                        && x.UserKey.Equals(PAYMENTID_VALID.ToString())),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result());
            mock.Setup(x => x.Send(
                    It.Is<GetPaymentByPayerIdUserKey.Query>(
                        x => x.PayerId.Equals(PAYERID_INVALID.ToString())
                        && x.UserKey.Equals(PAYMENTID_INVALID.ToString())),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result)null);

            // GetInboundPayment
            mock.Setup(x => x.Send(
                    It.Is<GetPaymentByInboundPaymentId.Query>(x => x.InboundPaymentId == PAYMENTID_VALID),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetInboundPaymentWithClobByIdSql.Result());
            mock.Setup(x => x.Send(
                    It.Is<GetPaymentByInboundPaymentId.Query>(x => x.InboundPaymentId == PAYMENTID_INVALID),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetInboundPaymentWithClobByIdSql.Result)null);

            // GetInboundHeader
            mock.Setup(x => x.Send(
                    It.Is<GetHeaderByPaymentId.Query>(x => x.InboundPaymentId == PAYMENTID_VALID),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InboundHeader());
            mock.Setup(x => x.Send(
                    It.Is<GetHeaderByPaymentId.Query>(x => x.InboundPaymentId == PAYMENTID_INVALID),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((InboundHeader)null);

            return mock.Object;
        }

        public InboundPaymentControllerFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services
                    .AddSingleton<InboundPaymentController>()
                    .AddSingleton(MockMediator());
            });

        [Fact]
        public async Task GetPaymentBatch_Should_Return_404NotFound_When_Null_Results()
        {
            // arrange
            var controller = _container.Get<InboundPaymentController>();

            // act
            async Task<ActionResult<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>> act() =>
                await controller.GetPaymentBatch(PAYERID_INVALID.ToString(), PAYMENTID_INVALID.ToString(), CancellationToken.None);

            // assert
            (await act()).Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetPaymentBatch_Should_Return_200OK()
        {
            // arrange
            var controller = _container.Get<InboundPaymentController>();

            // act
            async Task<ActionResult<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>> act() =>
                await controller.GetPaymentBatch(PAYERID_VALID.ToString(), PAYMENTID_VALID.ToString(), CancellationToken.None);

            // assert
            (await act()).Value.Should().BeOfType<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>();
        }

        [Fact]
        public async Task GetInboundPayment_Should_Return_404NotFound_When_Null_Results()
        {
            // arrange
            var controller = _container.Get<InboundPaymentController>();

            // act
            async Task<ActionResult<GetInboundPaymentWithClobByIdSql.Result>> act() =>
                await controller.GetInboundPayment(PAYMENTID_INVALID, CancellationToken.None);

            // assert
            (await act()).Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetInboundPayment_Should_Return_200OK()
        {
            // arrange
            var controller = _container.Get<InboundPaymentController>();

            // act
            async Task<ActionResult<GetInboundPaymentWithClobByIdSql.Result>> act() =>
                await controller.GetInboundPayment(PAYMENTID_VALID, CancellationToken.None);

            // assert
            (await act()).Value.Should().BeOfType<GetInboundPaymentWithClobByIdSql.Result>();
        }

        [Fact]
        public async Task GetInboundHeader_Should_Return_404NotFound_When_Null_Results()
        {
            // arrange
            var controller = _container.Get<InboundPaymentController>();

            // act
            async Task<ActionResult<InboundHeader>> act() =>
                await controller.GetInboundHeader(PAYMENTID_INVALID, CancellationToken.None);

            // assert
            (await act()).Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetInboundHeader_Should_Return_200OK()
        {
            // arrange
            var controller = _container.Get<InboundPaymentController>();

            // act
            async Task<ActionResult<InboundHeader>> act() =>
                await controller.GetInboundHeader(PAYMENTID_VALID, CancellationToken.None);

            // assert
            (await act()).Value.Should().BeOfType<InboundHeader>();
        }
    }
}
