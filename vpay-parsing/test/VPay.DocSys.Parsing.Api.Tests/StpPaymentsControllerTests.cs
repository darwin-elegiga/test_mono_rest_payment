using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Api.Controllers;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using Xunit;

namespace VPay.DocSys.Parsing.Api.Tests
{
    /// <summary>
    /// Unit tests for <see cref="StpPaymentsController"/> class.
    /// </summary>
    public class StpPaymentsControllerTests
    {
        private readonly IStpPaymentService _stpPaymentService = new Mock<IStpPaymentService>(MockBehavior.Strict).Object;
        private readonly IPlatformConfigurationApi _platformConfigurationApi = new Mock<IPlatformConfigurationApi>(MockBehavior.Strict).Object;
        private readonly StpPaymentsController _controller;

        public StpPaymentsControllerTests()
        {
            _controller = new StpPaymentsController(
            _stpPaymentService,
            _platformConfigurationApi);
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
                    .ReturnsAsync(stpPayment);

                var result = await _controller.GetStpPayment(1);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is OkObjectResult);

                var okResult = result.Result as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.NotNull(okResult.Value);
                Assert.True(okResult.Value is StpPaymentDto);

                var item = okResult.Value as StpPaymentDto;
                Assert.Equivalent(stpPaymentDto, item);
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

        public class InsertStpPayments : StpPaymentsControllerTests
        {
            [Fact]
            public async Task ReturnsAsyncBadRequestWhenStpPaymentsIsNull()
            {
                var result = await _controller.InsertStpPayments(null);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.Equal("The group criteria is required.", badRequestResult.Value);
            }

            [Fact]
            public async Task ReturnsAsyncBadRequestWhenGroupCriteriaIsInvalid()
            {
                var groupDto = MockStpGroup();
                groupDto.TransactionIds = new List<long> { 0 };

                var result = await _controller.InsertStpPayments(groupDto);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.StartsWith("Validation failed:", badRequestResult.Value.ToString());
            }

            [Fact]
            public async Task ReturnsAsyncBadRequestWhenNetworkEndpointReturnsAsyncNull()
            {
                var groupDto = MockStpGroup();

                NetworkDto network = null;
                Mock.Get(_platformConfigurationApi).Setup(m => m.GetNetworkPartner(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(network);

                var result = await _controller.InsertStpPayments(groupDto);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.StartsWith("The network partner does not exist", badRequestResult.Value.ToString());
            }

            [Fact]
            public async Task ReturnsAsyncBadRequestWhenNetworkApiThrowsNotFound()
            {
                var groupDto = MockStpGroup();
                var expectedException = Refit.ApiException.Create("Some error", new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(System.Net.HttpStatusCode.NotFound), null, null);

                Mock.Get(_platformConfigurationApi)
                    .Setup(m => m.GetNetworkPartner(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(expectedException.Result);

                var result = await _controller.InsertStpPayments(groupDto);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);
                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.StartsWith("The network partner does not exist", badRequestResult.Value.ToString());
            }

            [Fact]
            public async Task ReturnsAsyncStatusCode500WhenNetworkApiThrowsOtherApiException()
            {
                var groupDto = MockStpGroup();
                var expectedException = Refit.ApiException.Create("Some error", new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError), null, null);

                Mock.Get(_platformConfigurationApi)
                    .Setup(m => m.GetNetworkPartner(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(expectedException.Result);

                var result = await _controller.InsertStpPayments(groupDto);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is ObjectResult);
                var objectResult = result.Result as ObjectResult;
                Assert.Equal(500, objectResult.StatusCode);
                Assert.Contains("Platform Config API Error", objectResult.Value.ToString());
            }

            [Fact]
            public async Task ReturnsAsyncInsertedStpPayments()
            {
                var groupDto = MockStpGroup();
                var stpPayments = MockStpPayments(5).ToList();

                var network = new NetworkDto
                {
                    NetworkId = 1,
                    NetworkPartner = groupDto.NetworkName,
                    NetworkType = "STP",
                };

                var createdGroupDto = new StpPaymentCreatedGroupDto
                {
                    NetworkName = groupDto.NetworkName,
                    EnrollType = groupDto.EnrollType,
                    Edi835Type = groupDto.Edi835Type,
                    PaymentCount = groupDto.TransactionIds.Count(),
                    IsSuccessful = true
                };

                Mock.Get(_platformConfigurationApi).Setup(m => m.GetNetworkPartner(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(network);

                Mock.Get(_stpPaymentService).Setup(m => m.InsertStpPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), default))
                    .ReturnsAsync(stpPayments.Count);

                var result = await _controller.InsertStpPayments(groupDto);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is OkObjectResult);

                var okResult = result.Result as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.NotNull(okResult.Value);
                Assert.Equivalent(okResult.Value, new StpPaymentCreatedGroupDto
                {
                    NetworkName = groupDto.NetworkName,
                    EnrollType = groupDto.EnrollType,
                    Edi835Type = groupDto.Edi835Type,
                    PaymentCount = stpPayments.Count,
                    IsSuccessful = true
                });
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                var item = okResult.Value as StpPaymentDto;
                Assert.Equivalent(stpPaymentDto, item);
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                var item = okResult.Value as StpPaymentDto;
                Assert.Equivalent(stpPaymentDto, item);
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                var item = okResult.Value as StpPaymentDto;
                Assert.Equivalent(stpPaymentDto, item);
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                Mock.Get(_stpPaymentService).Setup(m => m.GetStpPayment(1, false, default))
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

                var item = okResult.Value as StpPaymentDto;
                Assert.Equivalent(stpPaymentDto, item);
            }
        }

        public class GetNetworkPartner : StpPaymentsControllerTests
        {
            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            public async Task ReturnsAsyncBadRequestWhenNetworkPartnerIsNullOrEmpty(string networkName)
            {
                var result = await _controller.GetNetworkPartner(networkName);

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.Equal("The network partner must be provided.", badRequestResult.Value);
            }

            [Fact]
            public async Task ReturnsAsyncNotFoundWhenNotFound()
            {
                NetworkDto networkPartner = null;

                Mock.Get(_platformConfigurationApi)
                    .Setup(m => m.GetNetworkPartner(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(networkPartner);

                var result = await _controller.GetNetworkPartner("TestNetwork");

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NotFoundResult);
            }

            [Fact]
            public async Task ReturnsAsyncNotFoundWhenNetworkApiThrowsApiException()
            {
                var expectedException = Refit.ApiException.Create("Some error", new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(System.Net.HttpStatusCode.NotFound), null, null);

                Mock.Get(_platformConfigurationApi)
                    .Setup(m => m.GetNetworkPartner(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(expectedException.Result);

                var result = await _controller.GetNetworkPartner("TestNetwork");

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NotFoundResult);
            }

            [Fact]
            public async Task ReturnsAsyncNetworkPartnerWhenFound()
            {
                NetworkDto networkPartner = new()
                {
                    NetworkId = 1,
                    NetworkPartner = "TestNetwork",
                    NetworkType = "STP",
                    CreatedBy = "UnitTest",
                    CreatedDateTime = DateTime.Today,
                    LastModifiedBy = "UnitTest",
                    LastModifiedDateTime = DateTime.Today
                };

                Mock.Get(_platformConfigurationApi)
                    .Setup(m => m.GetNetworkPartner(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(networkPartner);

                var result = await _controller.GetNetworkPartner("TestNetwork");
                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is OkObjectResult);

                var okResult = result.Result as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.NotNull(okResult.Value);
                Assert.True(okResult.Value is NetworkDto);

                var item = okResult.Value as NetworkDto;
                Assert.Equivalent(networkPartner, item);
            }
        }

        public class GetPrsConfiguration : StpPaymentsControllerTests
        {
            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            public async Task ReturnsAsyncBadRequestWhenOriginCodeIsNullOrEmpty(string originCode)
            {
                var result = await _controller.GetPrsConfiguration(originCode, "ABC", "ABC0001001");

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.Equal("The origin code must be provided.", badRequestResult.Value);
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            public async Task ReturnsAsyncBadRequestWhenClientCodeIsNullOrEmpty(string clientCode)
            {
                var result = await _controller.GetPrsConfiguration("SEMED", clientCode, "ABC0001001");

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is BadRequestObjectResult);

                var badRequestResult = result.Result as BadRequestObjectResult;
                Assert.NotNull(badRequestResult);
                Assert.Equal("The client code must be provided.", badRequestResult.Value);
            }

            [Fact]
            public async Task ReturnsAsyncNullWhenNotFound()
            {
                OptumPrsConfigurationDto prsConfiguration = null;

                Mock.Get(_platformConfigurationApi)
                    .Setup(m => m.GetOptumPrsConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsConfiguration);

                var result = await _controller.GetPrsConfiguration("SEMED", "ABC", "ABC0001001");

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NotFoundResult);
            }

            [Fact]
            public async Task ReturnsAsyncStatusCode500WhenPrsApiThrowsApiException()
            {
                var expectedException = Refit.ApiException.Create("Some error", new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(System.Net.HttpStatusCode.NotFound), null, null);

                Mock.Get(_platformConfigurationApi)
                    .Setup(m => m.GetOptumPrsConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(expectedException.Result);

                var result = await _controller.GetPrsConfiguration("SEMED", "ABC", "ABC0001001");

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is NotFoundResult);
            }

            [Fact]
            public async Task ReturnsAsyncPrsConfigurationWhenFound()
            {
                OptumPrsConfigurationDto prsConfiguration = new()
                {
                    OriginCode = "SEMED",
                    ClientCode = "ABC",
                    BillingEntityCode = "ABC0001001",
                    IsPrsEnabled = true
                };
                PrsConfigurationDto resultDto = new()
                {
                    OriginCode = "SEMED",
                    ClientCode = "ABC",
                    BillingEntityCode = "ABC0001001",
                    IsPrsEnabled = true
                };

                Mock.Get(_platformConfigurationApi)
                    .Setup(m => m.GetOptumPrsConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsConfiguration);

                var result = await _controller.GetPrsConfiguration("SEMED", "ABC", "ABC0001001");

                Assert.NotNull(result);
                Assert.NotNull(result.Result);
                Assert.True(result.Result is OkObjectResult);

                var okResult = result.Result as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.NotNull(okResult.Value);
                Assert.True(okResult.Value is PrsConfigurationDto);

                var item = okResult.Value as PrsConfigurationDto;
                Assert.Equivalent(resultDto, item);
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

        private static StpPaymentGroupDto MockStpGroup(
            string networkName = "SomeNetwork",
            string enrollType = "B2B",
            string edi835Type = "SDS")
        {
            return new StpPaymentGroupDto
            {
                NetworkName = networkName,
                EnrollType = enrollType,
                Edi835Type = edi835Type,
                TransactionIds = new List<long> { 1, 2, 3 }
            };
        }
    }
}
