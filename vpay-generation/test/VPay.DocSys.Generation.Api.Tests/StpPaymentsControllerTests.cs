using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Api.Controllers;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Common.Models;
using Xunit;

namespace VPay.DocSys.Parsing.Api.Tests.Controllers
{
    /// <summary>
    /// Unit tests for <see cref="StpPaymentsController"/> class.
    /// </summary>
    public class StpPaymentsControllerTests
    {
        private readonly IStpPaymentService _stpPaymentService = new Mock<IStpPaymentService>(MockBehavior.Strict).Object;
        private readonly StpPaymentsController _controller;

        public StpPaymentsControllerTests()
        {
            _controller = new StpPaymentsController(
            _stpPaymentService);
        }

        public class GetStpPayment : StpPaymentsControllerTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(-1)]
            public async Task ReturnsAsyncBadRequestWhenTransactionIdIsInvalid(long transactionId)
            {
                var result = await _controller.GetStpPayment(transactionId);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.Equal("The transaction Id must be greater than zero.", badRequestResult.Value);
            }

            [Fact]
            public async Task ReturnsAsyncNullWhenNotFound()
            {
                StpPayment stpPayment = null;

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                var result = await _controller.GetStpPayment(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NotFoundResult);
            }

            [Fact]
            public async Task ReturnsAsyncStpPaymentWhenFound()
            {
                StpPayment stpPayment = MockStpPayment(1);
                StpPaymentDto stpPaymentDto = MockStpPaymentDto(1);

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                var result = await _controller.GetStpPayment(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is OkObjectResult);

                var okResult = result.Result as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.NotNull(okResult.Value);
                Assert.True(okResult.Value is StpPaymentDto);

                var dto = okResult.Value as StpPaymentDto;
                Assert.Equivalent(stpPaymentDto, dto);
            }
        }

        public class SearchStpPayments : StpPaymentsControllerTests
        {
            [Fact]
            public async Task ReturnsAsyncBadRequestWhenSearchCriteriaIsInvalid()
            {
                var searchDto = new StpPaymentSearchDto
                {
                    NetworkName = null,
                    EnrollType = null,
                    Edi835Type = null,
                    Edi835Avail = null,
                    RemitValid = null,
                    PaymentSent = null,
                    PaymentSentRetries = null,
                    MaxResults = -1
                };

                var result = await _controller.SearchStpPayments(searchDto);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.StartsWith("Validation failed:", badRequestResult.Value.ToString());
            }

            [Fact]
            public async Task ReturnsAsyncEmptyListWhenNoneFound()
            {
                var stpPayments = new List<StpPayment>();

                var searchDto = new StpPaymentSearchDto
                {
                    NetworkName = null,
                    EnrollType = null,
                    Edi835Type = null,
                    Edi835Avail = null,
                    RemitValid = null,
                    PaymentSent = null,
                    PaymentSentRetries = null,
                    MaxResults = 100
                };

                Mock.Get(_stpPaymentService).Setup(m => m.SearchStpPayments(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool?>(),
                        It.IsAny<bool?>(),
                        It.IsAny<bool?>(),
                        It.IsAny<bool?>(),
                        It.IsAny<int?>(),
                        It.IsAny<int>(),
                        default))
                    .ReturnsAsync(stpPayments);
                var result = await _controller.SearchStpPayments(searchDto);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is OkObjectResult);

                var okResult = result.Result as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.NotNull(okResult.Value);
                Assert.True(okResult.Value is IEnumerable<StpPaymentDto>);

                var list = okResult.Value as IEnumerable<StpPaymentDto>;
                Assert.NotNull(list);
                Assert.Empty(list);
            }

            [Fact]
            public async Task ReturnsAsyncListWhenSomeFound()
            {
                var stpPayments = MockStpPayments(5).ToList();

                Mock.Get(_stpPaymentService).Setup(m => m.SearchStpPayments(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool?>(),
                        It.IsAny<bool?>(),
                        It.IsAny<bool?>(),
                        It.IsAny<bool?>(),
                        It.IsAny<int?>(),
                        It.IsAny<int>(),
                        default))
                    .ReturnsAsync(stpPayments);

                var searchDto = new StpPaymentSearchDto
                {
                    NetworkName = null,
                    EnrollType = null,
                    Edi835Type = null,
                    Edi835Avail = null,
                    RemitValid = null,
                    PaymentSent = null,
                    PaymentSentRetries = null,
                    MaxResults = 100
                };

                var result = await _controller.SearchStpPayments(searchDto);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is OkObjectResult);

                var okResult = result.Result as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.NotNull(okResult.Value);
                Assert.True(okResult.Value is IEnumerable<StpPaymentDto>);

                var list = okResult.Value as IEnumerable<StpPaymentDto>;
                Assert.NotNull(list);
                Assert.Equal(stpPayments.Count, list.Count());
            }
        }

        public class MarkStpPaymentRemitValid : StpPaymentsControllerTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(-1)]
            public async Task ReturnsAsyncBadRequestWhenTransactionIdIsInvalid(long transactionId)
            {
                var result = await _controller.MarkStpPaymentRemitValid(transactionId);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.Equal("The transaction Id must be greater than zero.", badRequestResult.Value);
            }

            [Fact]
            public async Task ReturnsAsyncNotFoundWhenNotFound()
            {
                StpPayment stpPayment = null;

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkRemitValid(1, null, default))
                    .ReturnsAsync(stpPayment);

                var result = await _controller.MarkStpPaymentRemitValid(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NotFoundResult);
            }

            [Fact]
            public async Task ReturnsAsyncNoContentWhenFoundButNotUpdated()
            {
                StpPayment stpPayment = MockStpPayment(1, remitValid: true);

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkRemitValid(1, null, default))
                    .ReturnsAsync((StpPayment)null);

                var result = await _controller.MarkStpPaymentRemitValid(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NoContentResult);
            }

            [Fact]
            public async Task ReturnsAsyncStpPaymentWhenFoundAndUpdated()
            {
                StpPayment stpPayment = MockStpPayment(1, remitValid: true);
                StpPaymentDto stpPaymentDto = MockStpPaymentDto(1, remitValid: true);

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkRemitValid(1, null, default))
                    .ReturnsAsync(stpPayment);

                var result = await _controller.MarkStpPaymentRemitValid(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is OkObjectResult);

                var okResult = result.Result as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.NotNull(okResult.Value);
                Assert.True(okResult.Value is StpPaymentDto);

                var dto = okResult.Value as StpPaymentDto;
                Assert.Equivalent(stpPaymentDto, dto);
            }
        }

        public class MarkStpPaymentEdi835Avail : StpPaymentsControllerTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(-1)]
            public async Task ReturnsAsyncBadRequestWhenTransactionIdIsInvalid(long transactionId)
            {
                var result = await _controller.MarkStpPaymentEdi835Avail(transactionId);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.Equal("The transaction Id must be greater than zero.", badRequestResult.Value);
            }

            [Fact]
            public async Task ReturnsAsyncNullWhenNotFound()
            {
                StpPayment stpPayment = null;

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkEdi835Avail(1, null, default))
                    .ReturnsAsync(stpPayment);

                var result = await _controller.MarkStpPaymentEdi835Avail(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NotFoundResult);
            }

            [Fact]
            public async Task ReturnsAsyncNoContentWhenFoundButNotUpdated()
            {
                StpPayment stpPayment = MockStpPayment(1, edi835Avail: true);

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkEdi835Avail(1, null, default))
                    .ReturnsAsync((StpPayment)null);

                var result = await _controller.MarkStpPaymentEdi835Avail(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NoContentResult);
            }

            [Fact]
            public async Task ReturnsAsyncStpPaymentWhenFoundAndUpdated()
            {
                StpPayment stpPayment = MockStpPayment(1, edi835Avail: true);
                StpPaymentDto stpPaymentDto = MockStpPaymentDto(1, edi835Avail: true);

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkEdi835Avail(1, null, default))
                    .ReturnsAsync(stpPayment);

                var result = await _controller.MarkStpPaymentEdi835Avail(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is OkObjectResult);

                var okResult = result.Result as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.NotNull(okResult.Value);
                Assert.True(okResult.Value is StpPaymentDto);

                var dto = okResult.Value as StpPaymentDto;
                Assert.Equivalent(stpPaymentDto, dto);
            }
        }

        public class MarkStpPaymentPaymentSent : StpPaymentsControllerTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(-1)]
            public async Task ReturnsAsyncBadRequestWhenTransactionIdIsInvalid(long transactionId)
            {
                var result = await _controller.MarkStpPaymentPaymentSent(transactionId);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.Equal("The transaction Id must be greater than zero.", badRequestResult.Value);
            }

            [Fact]
            public async Task ReturnsAsyncNotFoundWhenNotFound()
            {
                StpPayment stpPayment = null;

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkPaymentSent(1, null, default))
                    .ReturnsAsync(stpPayment);

                var result = await _controller.MarkStpPaymentPaymentSent(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NotFoundResult);
            }

            [Fact]
            public async Task ReturnsAsyncNoContentWhenFoundButNotUpdated()
            {
                StpPayment stpPayment = MockStpPayment(1, paymentSent: true);

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkPaymentSent(1, null, default))
                    .ReturnsAsync((StpPayment)null);

                var result = await _controller.MarkStpPaymentPaymentSent(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NoContentResult);
            }

            [Fact]
            public async Task ReturnsAsyncStpPaymentWhenFoundAndUpdated()
            {
                StpPayment stpPayment = MockStpPayment(1, paymentSent: true);
                StpPaymentDto stpPaymentDto = MockStpPaymentDto(1, paymentSent: true);

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkPaymentSent(1, null, default))
                    .ReturnsAsync(stpPayment);

                var result = await _controller.MarkStpPaymentPaymentSent(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is OkObjectResult);

                var okResult = result.Result as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.NotNull(okResult.Value);
                Assert.True(okResult.Value is StpPaymentDto);

                var dto = okResult.Value as StpPaymentDto;
                Assert.Equivalent(stpPaymentDto, dto);
            }
        }

        public class MarkStpPaymentPaymentNotSent : StpPaymentsControllerTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(-1)]
            public async Task ReturnsAsyncBadRequestWhenTransactionIdIsInvalid(long transactionId)
            {
                var result = await _controller.MarkStpPaymentPaymentNotSent(transactionId);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.Equal("The transaction Id must be greater than zero.", badRequestResult.Value);
            }

            [Fact]
            public async Task ReturnsAsyncNotFoundWhenNotFound()
            {
                StpPayment stpPayment = null;

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkPaymentNotSent(1, null, default))
                    .ReturnsAsync(stpPayment);

                var result = await _controller.MarkStpPaymentPaymentNotSent(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NotFoundResult);
            }

            [Fact]
            public async Task ReturnsAsyncNoContentWhenFoundButNotUpdated()
            {
                StpPayment stpPayment = MockStpPayment(1, paymentSent: true);

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkPaymentNotSent(1, null, default))
                    .ReturnsAsync((StpPayment)null);

                var result = await _controller.MarkStpPaymentPaymentNotSent(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NoContentResult);
            }

            [Fact]
            public async Task ReturnsAsyncStpPaymentWhenFoundAndUpdated()
            {
                StpPayment stpPayment = MockStpPayment(1, paymentSent: true);
                StpPaymentDto stpPaymentDto = MockStpPaymentDto(1, paymentSent: true);

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, default))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_stpPaymentService).Setup(m => m.MarkPaymentNotSent(1, null, default))
                    .ReturnsAsync(stpPayment);

                var result = await _controller.MarkStpPaymentPaymentNotSent(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is OkObjectResult);

                var okResult = result.Result as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.NotNull(okResult.Value);
                Assert.True(okResult.Value is StpPaymentDto);

                var dto = okResult.Value as StpPaymentDto;
                Assert.Equivalent(stpPaymentDto, dto);
            }
        }

        private static IEnumerable<StpPayment> MockStpPayments(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return MockStpPayment(i);
            }
        }

        private static StpPayment MockStpPayment(
            long transactionId,
            string enrollType = "B2B",
            string edi835Type = "SDS",
            bool remitValid = false,
            bool edi835Avail = false,
            bool paymentSent = false,
            bool pmbMessageSent = false)
        {
            StpPayment stpPayment = new()
            {
                TransactionId = transactionId,
                NetworkName = "TestNetwork",
                EnrollType = enrollType,
                Edi835Type = edi835Type,
                RemitValid = remitValid,
                Edi835Avail = edi835Avail,
                PaymentSent = paymentSent,
                PmbMessageSent = pmbMessageSent,
                PaymentSentRetries = 0,
                RemitValidDateTime = remitValid ? DateTime.Today : null,
                Edi835AvailDateTime = edi835Avail ? DateTime.Today : null,
                PaymentSentDateTime = paymentSent ? DateTime.Today : null,
                PmbMessageSentDateTime = pmbMessageSent ? DateTime.Today : null,
                CreatedUser = "UnitTest",
                CreatedDateTime = DateTime.Today,
                LastModifiedUser = "UnitTest",
                LastModifiedDateTime = DateTime.Today
            };

            return stpPayment;
        }

        private static StpPaymentDto MockStpPaymentDto(
            long transactionId,
            string enrollType = "B2B",
            string edi835Type = "SDS",
            bool remitValid = false,
            bool edi835Avail = false,
            bool paymentSent = false,
            bool pmbMessageSent = false)
        {
            StpPaymentDto stpPayment = new()
            {
                TransactionId = transactionId,
                NetworkName = "TestNetwork",
                EnrollType = enrollType,
                Edi835Type = edi835Type,
                RemitValid = remitValid,
                Edi835Avail = edi835Avail,
                PaymentSent = paymentSent,
                PmbMessageSent = pmbMessageSent,
                PaymentSentRetries = 0,
                RemitValidDateTime = remitValid ? DateTime.Today : null,
                Edi835AvailDateTime = edi835Avail ? DateTime.Today : null,
                PaymentSentDateTime = paymentSent ? DateTime.Today : null,
                PmbMessageSentDateTime = pmbMessageSent ? DateTime.Today : null,
                CreatedUser = "UnitTest",
                CreatedDateTime = DateTime.Today,
                LastModifiedUser = "UnitTest",
                LastModifiedDateTime = DateTime.Today
            };

            return stpPayment;
        }
    }
}
