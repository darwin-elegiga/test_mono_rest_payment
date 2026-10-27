using AutoMapper;
using FluentAssertions;
using FluentAssertions.Execution;
using GreenPipes;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Gateway.Common.Commands;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Paging;
using VPay.Gateway.Data.Sql.PepClaims;
using VPay.Gateway.Management.Api.Controllers;
using VPay.Gateway.Management.Api.Models;
using VPay.Gateway.Management.Application.GuidewirePayments;
using Xunit;

namespace VPay.Gateway.Management.Api.Tests.Controllers;
/// <summary>
/// Tests for <see cref="GuidewireController"/>
/// </summary>
public class GuidewireControllerTests
{
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMediator _mediator;
    private readonly GuidewireReplaySettings _settings;
    private readonly GuidewireController _controller;
    private readonly ILoggerFactory _loggerFactory = new LoggerFactory();
    public GuidewireControllerTests()
    {
        var profiles = from type in typeof(Startup).Assembly.GetTypes()
                       where typeof(Profile).IsAssignableFrom(type)
                       select type;

        var configuration = new MapperConfiguration(cfg =>
        {
            foreach (var profileType in profiles)
            {
                cfg.AddProfile(profileType);
            }
        },_loggerFactory);
        _mapper = configuration.CreateMapper();
        _publishEndpoint = Mock.Of<IPublishEndpoint>(MockBehavior.Strict);
        _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
        _settings = new GuidewireReplaySettings();
        _controller = new GuidewireController(_mapper, _publishEndpoint, _mediator, _settings);
    }

    public class Constructor : GuidewireControllerTests
    {
        [Fact]
        public void WhenMapperIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new GuidewireController(null!, _publishEndpoint, _mediator, _settings);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mapper");
        }

        [Fact]
        public void WhenPublishEndpointIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new GuidewireController(_mapper, null, _mediator, _settings);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("publishEndpoint");
        }

        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new GuidewireController(_mapper, _publishEndpoint, null, _settings);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mediator");
        }

        [Fact]
        public void WhenSettingsIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new GuidewireController(_mapper, _publishEndpoint, _mediator, null);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("settings");
        }
    }

    public class PostMessage : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsInvalid_ReturnsBadRequest()
        {
            const string ErrorField = "request";
            const string ErrorMessage = "Some error.";
            var errorMessage = $"{ErrorMessage} (Parameter '{ErrorField}')";

            var request = new GuidewirePaymentMessageDto()
            {
                ClientCode = "ABC",
                PaymentId = "P01",
                Status = SendGuidewirePaymentStatus.Issued
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<UpdateGuidewirePaymentStatusCommand>(), default))
                .ThrowsAsync(new ArgumentException(ErrorMessage, ErrorField))
                .Verifiable("Send Not Called");

            var result = await _controller.PostMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);

                _controller.ModelState.ValidationState.Should().Be(ModelValidationState.Invalid);

                var modelState = _controller.ModelState.FirstOrDefault();
                modelState.Should().NotBeNull();
                modelState.Key.Should().Be(ErrorField);
                modelState.Value.Errors.FirstOrDefault().ErrorMessage.Should().Be(errorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestIsValid_ReturnsResult()
        {
            var request = new GuidewirePaymentMessageDto()
            {
                ClientCode = "ABC",
                PaymentId = "P01",
                Status = SendGuidewirePaymentStatus.Issued
            };

            var response = new GuidewireResult()
            {
                Status = "VALID",
                Message = "Good"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<UpdateGuidewirePaymentStatusCommand>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.PostMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Value.Should().NotBeNull();
                result.Value.Should().Be(response);

                _controller.ModelState.ValidationState.Should().Be(ModelValidationState.Valid);

                var modelState = _controller.ModelState;
                modelState.Count.Should().Be(0);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    public class PostEnrollmentMessage : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ReturnsAccepted()
        {
            Mock.Get(_publishEndpoint)
                .Setup(x => x.Publish(It.IsAny<SendGuidewireMessage>(), It.IsAny<IPipe<PublishContext<SendGuidewireMessage>>>(), default))
                .Returns(Task.CompletedTask)
                .Verifiable("Publish Not Called");

            var result = await _controller.PostEnrollmentMessage(null);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Should().BeOfType<AcceptedResult>().Should().NotBeNull();

                var accepted = result.Should().BeOfType<AcceptedResult>().Subject;
                accepted.Should().NotBeNull();
                accepted.StatusCode.Should().Be(202);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestIsValid_ReturnsAccepted()
        {
            var request = new GuidewireEnrollmentStatusChangeDto()
            {
                ClientCode = "ABC",
                PayeeId = "P01",
                ClaimNumber = "E01",
                Status = ""
            };

            Mock.Get(_publishEndpoint)
                .Setup(x => x.Publish(It.IsAny<SendGuidewireMessage>(), It.IsAny<IPipe<PublishContext<SendGuidewireMessage>>>(), default))
                .Returns(Task.CompletedTask)
                .Verifiable("Publish Not Called");

            var result = await _controller.PostEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Should().BeOfType<AcceptedResult>().Should().NotBeNull();

                var accepted = result.Should().BeOfType<AcceptedResult>().Subject;
                accepted.Should().NotBeNull();
                accepted.StatusCode.Should().Be(202);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    public class SearchEnrollmentMessages : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ReturnsBadRequest()
        {
            const string ErrorParam = "message";
            var errorMessage = $"Value cannot be null. (Parameter '{ErrorParam}')";

            var result = await _controller.SearchEnrollmentMessages(null);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(errorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestThrowsException_ReturnsBadRequest()
        {
            const string MemberId = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<SearchGuidewireEnrollmentMessages.Query>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var request = new GuidewireEnrollmentStatusSearchDto
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001",
                ProcessStatus = "STARTED",
                Page = 1,
                PageSize = 10
            };

            var result = await _controller.SearchEnrollmentMessages(request, default);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItems_ReturnsEmptyList()
        {
            const string MemberId = "NON-EXISTENT";

            var items = new List<EnrollmentStatus>();
            var pageModel = new PageModel
            {
                CurrentPage = 1,
                PageSize = 1,
                TotalRows = 1
            };

            var request = new GuidewireEnrollmentStatusSearchDto
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001",
                ProcessStatus = "STARTED",
                Page = 1,
                PageSize = 10
            };

            var response = new PagedResult<EnrollmentStatus>(items, pageModel);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<SearchGuidewireEnrollmentMessages.Query>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.SearchEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Value.Should().BeOfType<Pagination<GuidewireEnrollmentStatusDto>>();

                var dtoList = okResult.Value as Pagination<GuidewireEnrollmentStatusDto>;
                dtoList.Should().NotBeNull();
                dtoList.Items.Count.Should().Be(0);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Theory]
        [InlineData(1, 1, 1, 1)]
        [InlineData(10, 10, 1, 10)]
        [InlineData(15, 5, 2, 10)]
        [InlineData(15, 0, 3, 10)]
        public async Task WhenRequestMatchesItems_ReturnsItemList(int totalRows, int itemCount, int page, int pageSize)
        {
            const string MemberId = "GOOD MEMBER";

            var request = new GuidewireEnrollmentStatusSearchDto
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001",
                ProcessStatus = "STARTED",
                Page = page,
                PageSize = pageSize
            };

            var response = MockEnrollmentStatus(totalRows, itemCount, page, pageSize);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<SearchGuidewireEnrollmentMessages.Query>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.SearchEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Value.Should().BeOfType<Pagination<GuidewireEnrollmentStatusDto>>();

                var itemList = response.Items;
                var dtoList = okResult.Value as Pagination<GuidewireEnrollmentStatusDto>;
                dtoList.Should().NotBeNull();
                dtoList.CurrentPage.Should().Be(page);
                dtoList.PageSize.Should().Be(pageSize);
                dtoList.TotalRows.Should().Be(totalRows);
                dtoList.Items.Should().NotBeNull();
                dtoList.Items.Count.Should().Be(itemCount);
                for (int i = 0; i < dtoList.Items.Count; i++)
                {
                    var dto = dtoList.Items[i];
                    var items = response.Items.ToList();
                    dto.PayeeStatusId.Should().Be(items[i].Id);
                    dto.ClientCode.Should().Be(items[i].ClientCode);
                    dto.MemberId.Should().Be(items[i].MemberId);
                    dto.PayeeId.Should().Be(items[i].PayeeId);
                    dto.ClaimId.Should().Be(items[i].ClaimId);
                    dto.ProcessStatus.Should().Be(items[i].CurrentProcessStatus);
                    dto.PepPayeeId.Should().Be(items[i].PepPayeeId);
                    dto.PepStatusId.Should().Be(items[i].PepStatusId);
                    dto.StatusDateTime.Should().Be(items[i].StatusDateTime);
                    dto.CreatedDateTime.Should().Be(items[i].CreatedDateTime);
                    dto.LastModifiedDateTime.Should().Be(items[i].LastModifiedDateTime);
                }
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    public class SearchStuckEnrollmentMessages : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ReturnsBadRequest()
        {
            const string ErrorParam = "message";
            var errorMessage = $"Value cannot be null. (Parameter '{ErrorParam}')";
            var result = await _controller.SearchStuckEnrollmentMessages(null);
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();
                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(errorMessage);
            }
            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestThrowsException_ReturnsBadRequest()
        {
            const string MemberId = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<SearchStuckGuidewireEnrollmentMessages.Query>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var request = new GuidewireEnrollmentStatusStuckSearchDto
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001",
                Page = 1,
                PageSize = 10
            };

            var result = await _controller.SearchStuckEnrollmentMessages(request, default);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItems_ReturnsEmptyList()
        {
            const string MemberId = "NON-EXISTENT";

            var items = new List<EnrollmentStatus>();
            var pageModel = new PageModel
            {
                CurrentPage = 1,
                PageSize = 1,
                TotalRows = 1
            };

            var request = new GuidewireEnrollmentStatusStuckSearchDto
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001",
                Page = 1,
                PageSize = 10
            };

            var response = new PagedResult<EnrollmentStatus>(items, pageModel);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<SearchStuckGuidewireEnrollmentMessages.Query>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.SearchStuckEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Value.Should().BeOfType<Pagination<GuidewireEnrollmentStatusDto>>();

                var dtoList = okResult.Value as Pagination<GuidewireEnrollmentStatusDto>;
                dtoList.Should().NotBeNull();
                dtoList.Items.Count.Should().Be(0);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Theory]
        [InlineData(1, 1, 1, 1)]
        [InlineData(10, 10, 1, 10)]
        [InlineData(15, 5, 2, 10)]
        [InlineData(15, 0, 3, 10)]
        public async Task WhenRequestMatchesItems_ReturnsItemList(int totalRows, int itemCount, int page, int pageSize)
        {
            const string MemberId = "GOOD MEMBER";

            var request = new GuidewireEnrollmentStatusStuckSearchDto
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001",
                Page = page,
                PageSize = pageSize
            };

            var response = MockEnrollmentStatus(totalRows, itemCount, page, pageSize);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<SearchStuckGuidewireEnrollmentMessages.Query>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.SearchStuckEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Value.Should().BeOfType<Pagination<GuidewireEnrollmentStatusDto>>();

                var itemList = response.Items;
                var dtoList = okResult.Value as Pagination<GuidewireEnrollmentStatusDto>;
                dtoList.Should().NotBeNull();
                dtoList.CurrentPage.Should().Be(page);
                dtoList.PageSize.Should().Be(pageSize);
                dtoList.TotalRows.Should().Be(totalRows);
                dtoList.Items.Should().NotBeNull();
                dtoList.Items.Count.Should().Be(itemCount);
                for (int i = 0; i < dtoList.Items.Count; i++)
                {
                    var dto = dtoList.Items[i];
                    var items = response.Items.ToList();
                    dto.PayeeStatusId.Should().Be(items[i].Id);
                    dto.ClientCode.Should().Be(items[i].ClientCode);
                    dto.MemberId.Should().Be(items[i].MemberId);
                    dto.PayeeId.Should().Be(items[i].PayeeId);
                    dto.ClaimId.Should().Be(items[i].ClaimId);
                    dto.ProcessStatus.Should().Be(items[i].CurrentProcessStatus);
                    dto.PepPayeeId.Should().Be(items[i].PepPayeeId);
                    dto.PepStatusId.Should().Be(items[i].PepStatusId);
                    dto.StatusDateTime.Should().Be(items[i].StatusDateTime);
                    dto.CreatedDateTime.Should().Be(items[i].CreatedDateTime);
                    dto.LastModifiedDateTime.Should().Be(items[i].LastModifiedDateTime);
                }
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    public class SearchFailedEnrollmentMessages : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ReturnsBadRequest()
        {
            const string ErrorParam = "message";
            var errorMessage = $"Value cannot be null. (Parameter '{ErrorParam}')";
            var result = await _controller.SearchFailedEnrollmentMessages(null);
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();
                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(errorMessage);
            }
            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestThrowsException_ReturnsBadRequest()
        {
            const string MemberId = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<SearchFailedGuidewireEnrollmentMessages.Query>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var request = new GuidewireEnrollmentStatusFailedSearchDto
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001",
                Page = 1,
                PageSize = 10
            };

            var result = await _controller.SearchFailedEnrollmentMessages(request, default);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItems_ReturnsEmptyList()
        {
            const string MemberId = "NON-EXISTENT";

            var items = new List<EnrollmentStatus>();
            var pageModel = new PageModel
            {
                CurrentPage = 1,
                PageSize = 1,
                TotalRows = 1
            };

            var request = new GuidewireEnrollmentStatusFailedSearchDto
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001",
                Page = 1,
                PageSize = 10
            };

            var response = new PagedResult<EnrollmentStatus>(items, pageModel);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<SearchFailedGuidewireEnrollmentMessages.Query>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.SearchFailedEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Value.Should().BeOfType<Pagination<GuidewireEnrollmentStatusDto>>();

                var dtoList = okResult.Value as Pagination<GuidewireEnrollmentStatusDto>;
                dtoList.Should().NotBeNull();
                dtoList.Items.Count.Should().Be(0);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Theory]
        [InlineData(1, 1, 1, 1)]
        [InlineData(10, 10, 1, 10)]
        [InlineData(15, 5, 2, 10)]
        [InlineData(15, 0, 3, 10)]
        public async Task WhenRequestMatchesItems_ReturnsItemList(int totalRows, int itemCount, int page, int pageSize)
        {
            const string MemberId = "GOOD MEMBER";

            var request = new GuidewireEnrollmentStatusFailedSearchDto
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001",
                Page = page,
                PageSize = pageSize
            };

            var response = MockEnrollmentStatus(totalRows, itemCount, page, pageSize);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<SearchFailedGuidewireEnrollmentMessages.Query>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.SearchFailedEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Value.Should().BeOfType<Pagination<GuidewireEnrollmentStatusDto>>();

                var itemList = response.Items;
                var dtoList = okResult.Value as Pagination<GuidewireEnrollmentStatusDto>;
                dtoList.Should().NotBeNull();
                dtoList.CurrentPage.Should().Be(page);
                dtoList.PageSize.Should().Be(pageSize);
                dtoList.TotalRows.Should().Be(totalRows);
                dtoList.Items.Should().NotBeNull();
                dtoList.Items.Count.Should().Be(itemCount);
                for (int i = 0; i < dtoList.Items.Count; i++)
                {
                    var dto = dtoList.Items[i];
                    var items = response.Items.ToList();
                    dto.PayeeStatusId.Should().Be(items[i].Id);
                    dto.ClientCode.Should().Be(items[i].ClientCode);
                    dto.MemberId.Should().Be(items[i].MemberId);
                    dto.PayeeId.Should().Be(items[i].PayeeId);
                    dto.ClaimId.Should().Be(items[i].ClaimId);
                    dto.ProcessStatus.Should().Be(items[i].CurrentProcessStatus);
                    dto.PepPayeeId.Should().Be(items[i].PepPayeeId);
                    dto.PepStatusId.Should().Be(items[i].PepStatusId);
                    dto.StatusDateTime.Should().Be(items[i].StatusDateTime);
                    dto.CreatedDateTime.Should().Be(items[i].CreatedDateTime);
                    dto.LastModifiedDateTime.Should().Be(items[i].LastModifiedDateTime);
                }
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    public class CompleteEnrollmentMessage : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ReturnsBadRequest()
        {
            const string ErrorParam = "message";
            var errorMessage = $"Value cannot be null. (Parameter '{ErrorParam}')";

            var result = await _controller.CompleteEnrollmentMessage(null);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(errorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestFails_ReturnsBadRequest()
        {
            const string MemberId = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var result = await _controller.CompleteEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItem_ReturnsOkNotFound()
        {
            const string MemberId = "NON-EXISTENT";
            const string ResultStatus = "COMPLETED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Record not found for Client '{request.ClientCode}', Member Id '{request.MemberId}', Claim Id '{request.ClaimNumber}'";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.CompleteEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItems_ReturnsOkItems()
        {
            const string MemberId = "GOOD-ENTRY";
            const string ResultStatus = "COMPLETED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Successfully reset Guidewire enrollment message to {ResultStatus} status (client {request.ClientCode}, member {request.MemberId}, claim {request.ClaimNumber})";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.CompleteEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    public class SkipEnrollmentMessage : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ReturnsBadRequest()
        {
            const string ErrorParam = "message";
            var errorMessage = $"Value cannot be null. (Parameter '{ErrorParam}')";

            var result = await _controller.SkipEnrollmentMessage(null);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(errorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestFails_ReturnsBadRequest()
        {
            const string MemberId = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var result = await _controller.SkipEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItem_ReturnsOkNotFound()
        {
            const string MemberId = "NON-EXISTENT";
            const string ResultStatus = "SKIPPED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Record not found for Client '{request.ClientCode}', Member Id '{request.MemberId}', Claim Id '{request.ClaimNumber}'";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.SkipEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItems_ReturnsOkItems()
        {
            const string MemberId = "GOOD-ENTRY";
            const string ResultStatus = "SKIPPED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Successfully reset Guidewire enrollment message to {ResultStatus} status (client {request.ClientCode}, member {request.MemberId}, claim {request.ClaimNumber})";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.SkipEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    public class FailEnrollmentMessage : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ReturnsBadRequest()
        {
            const string ErrorParam = "message";
            var errorMessage = $"Value cannot be null. (Parameter '{ErrorParam}')";

            var result = await _controller.ResetFailedEnrollmentMessage(null);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(errorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestFails_ReturnsBadRequest()
        {
            const string MemberId = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var result = await _controller.ResetFailedEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItem_ReturnsOkNotFound()
        {
            const string MemberId = "NON-EXISTENT";
            const string ResultStatus = "FAILED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Record not found for Client '{request.ClientCode}', Member Id '{request.MemberId}', Claim Id '{request.ClaimNumber}'";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.ResetFailedEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItems_ReturnsOkItems()
        {
            const string MemberId = "GOOD-ENTRY";
            const string ResultStatus = "FAILED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Successfully reset Guidewire enrollment message to {ResultStatus} status (client {request.ClientCode}, member {request.MemberId}, claim {request.ClaimNumber})";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.ResetFailedEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    public class ResetScheduledEnrollmentMessages : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ReturnsBadRequest()
        {
            const string ErrorParam = "message";
            var errorMessage = $"Value cannot be null. (Parameter '{ErrorParam}')";

            var result = await _controller.ResetScheduledEnrollmentMessage(null);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(errorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestFails_ReturnsBadRequest()
        {
            const string MemberId = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var result = await _controller.ResetScheduledEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItem_ReturnsOkNotFound()
        {
            const string MemberId = "NON-EXISTENT";
            const string ResultStatus = "COMPLETED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Record not found for Client '{request.ClientCode}', Member Id '{request.MemberId}', Claim Id '{request.ClaimNumber}'";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.ResetScheduledEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItems_ReturnsOkItems()
        {
            const string MemberId = "GOOD-ENTRY";
            const string ResultStatus = "COMPLETED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Successfully reset Guidewire enrollment message to {ResultStatus} status (client {request.ClientCode}, member {request.MemberId}, claim {request.ClaimNumber})";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.ResetScheduledEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    public class ResetSendingEnrollmentMessages : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ReturnsBadRequest()
        {
            const string ErrorParam = "message";
            var errorMessage = $"Value cannot be null. (Parameter '{ErrorParam}')";

            var result = await _controller.ResetSendingEnrollmentMessage(null);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(errorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestFails_ReturnsBadRequest()
        {
            const string MemberId = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var result = await _controller.ResetSendingEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItem_ReturnsOkNotFound()
        {
            const string MemberId = "NON-EXISTENT";
            const string ResultStatus = "COMPLETED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Record not found for Client '{request.ClientCode}', Member Id '{request.MemberId}', Claim Id '{request.ClaimNumber}'";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.ResetSendingEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItems_ReturnsOkItems()
        {
            const string MemberId = "GOOD-ENTRY";
            const string ResultStatus = "COMPLETED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Successfully reset Guidewire enrollment message to {ResultStatus} status (client {request.ClientCode}, member {request.MemberId}, claim {request.ClaimNumber})";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.ResetSendingEnrollmentMessage(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    public class ResetRetryCountForEnrollmentMessages : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ReturnsBadRequest()
        {
            const string ErrorParam = "message";
            var errorMessage = $"Value cannot be null. (Parameter '{ErrorParam}')";

            var result = await _controller.ResetRetryCountForEnrollmentMessages(null);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(errorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestFails_ReturnsBadRequest()
        {
            const string MemberId = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessageCount.Command>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var result = await _controller.ResetRetryCountForEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItem_ReturnsOkNotFound()
        {
            const string MemberId = "NON-EXISTENT";
            const string ResultStatus = "COMPLETED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Record not found for Client '{request.ClientCode}', Member Id '{request.MemberId}', Claim Id '{request.ClaimNumber}'";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessageCount.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.ResetRetryCountForEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItems_ReturnsOkItems()
        {
            const string MemberId = "GOOD-ENTRY";
            const string ResultStatus = "COMPLETED";

            var request = new GuidewireEnrollmentStatusResetDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = $"Successfully reset Guidewire enrollment message to {ResultStatus} status (client {request.ClientCode}, member {request.MemberId}, claim {request.ClaimNumber})";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ResetGuidewireEnrollmentMessageCount.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.ResetRetryCountForEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    public class ReplayEnrollmentMessages : GuidewireControllerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ReturnsBadRequest()
        {
            const string ErrorParam = "message";
            var errorMessage = $"Value cannot be null. (Parameter '{ErrorParam}')";

            var result = await _controller.ReplayEnrollmentMessages(null);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(errorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestFails_ReturnsBadRequest()
        {
            const string MemberId = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";

            var request = new GuidewireEnrollmentStatusReplayDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ReplayGuidewireEnrollmentMessages.Command>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var result = await _controller.ReplayEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().Be(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestUnableToReplay_ReturnsOkNotReplayed()
        {
            const string MemberId = "NON-EXISTENT";
            const string ResultStatus = "SCHEDULED";

            var request = new GuidewireEnrollmentStatusReplayDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = "E001"
            };

            var resultMessage = "Unable to replay processing status of Guidewire enrollment message(s)";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ReplayGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.ReplayEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItem_ReturnsOkNoItems()
        {
            const string MemberId = "NON-EXISTENT";
            const string ClaimId = "E001";
            const string ResultStatus = "SCHEDULED";

            var request = new GuidewireEnrollmentStatusReplayDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = ClaimId
            };

            var resultMessage = $"Successfully reset 0 Guidewire enrollment message(s) to {ResultStatus} status ()";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ReplayGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.ReplayEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItems_ReturnsOkItems()
        {
            const string MemberId = "GOOD-ENTRY";
            const string ClaimId = "E001";
            const string ResultStatus = "SCHEDULED";

            var request = new GuidewireEnrollmentStatusReplayDto()
            {
                ClientCode = "ABC",
                MemberId = MemberId,
                ClaimNumber = ClaimId
            };

            var resultMessage = $"Successfully reset 1 Guidewire enrollment message(s) to {ResultStatus} status ({ClaimId})";

            var response = new GuidewireResult()
            {
                Status = ResultStatus,
                Message = resultMessage
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<ReplayGuidewireEnrollmentMessages.Command>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.ReplayEnrollmentMessages(request);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.StatusCode.Should().Be(200);
                okResult.Value.Should().Be(resultMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    protected static PagedResult<EnrollmentStatus> MockEnrollmentStatus(
        int totalRows,
        int itemCount,
        int page,
        int pageSize)
    {
        var pageModel = new PageModel
        {
            CurrentPage = page,
            PageSize = pageSize,
            TotalRows = totalRows
        };

        var items = new List<EnrollmentStatus>();

        for (int i = 0; i < itemCount; i++)
        {
            items.Add(new EnrollmentStatus
            {
                Id = i,
                ClientCode = "ABC",
                MemberId = $"M0{i}",
                PayeeId = 1,
                ClaimId = $"E00{i}",
                CurrentProcessStatus = "STARTED",
                PepPayeeId = 1,
                PepStatusId = 1,
                StatusDateTime = DateTime.Today,
                CreatedDateTime = DateTime.Today,
                LastModifiedDateTime = DateTime.Today
            });
        }

        var result = new PagedResult<EnrollmentStatus>(items, pageModel);
        return result;
    }
}
