using FluentAssertions;
using FluentAssertions.Execution;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Api.Controllers;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Application;
using VPay.Gateway.Common.Preferencing.Models;
using Xunit;

namespace VPay.Gateway.Api.Tests.Controllers;

/// <summary>
/// Tests for <see cref="NetworksController"/>
/// </summary>
public class NetworkControllerTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<EnrollmentsController> _logger;
    private readonly NetworksController _controller;

    private const string CorrelationId = "123456789-123456789";
    private const string NetworkPartner = "VALID_NAME";
    private const string ProviderTin = "987654321";
    private const string ProviderNpi = "1234567890";
    private const string ProviderName = "LEGAL ENTITY NAME";
    private const string ProviderDba = "";
    private const string EnrollmentType = "C2B";
    private const string ClientCode = "ABC";
    private const string BeCode = "ABC001001";

    public NetworkControllerTests()
    {
        _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
        _logger = new NullLogger<EnrollmentsController>();
        _controller = new NetworksController(_mediator, _logger);
    }

    #region Get Network
    public class GetNetwork : NetworkControllerTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task WhenNetworkPartnerIsNotProvided_ReturnsBadRequest(string networkPartner)
        {
            var result = await _controller.GetNetwork(networkPartner);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);

                var resultMsg = badResult.Value as string;
                resultMsg.Should().NotBeNull();
                resultMsg.Should().StartWith("Network Partner Name Or Network Id must be provided");
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenNetworkPartnerIsInvalid_ReturnsNotFound()
        {
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync((NetworkDto)null)
                .Verifiable();

            var result = await _controller.GetNetwork(NetworkPartner);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<NotFoundResult>().Should().NotBeNull();

                var notResult = result.Result.Should().BeOfType<NotFoundResult>().Subject;
                notResult.Should().NotBeNull();
                notResult.StatusCode.Should().Be(404);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenNetworkPartnerIsActive_ReturnsOkActive()
        {
            var network = MockNetwork("A");

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(network)
                .Verifiable();

            var result = await _controller.GetNetwork(NetworkPartner);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<OkObjectResult>().Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Should().NotBeNull();
                okResult.StatusCode.Should().Be(200);

                var response = okResult.Value as NetworkResultDto;
                response.Should().NotBeNull();
                response.NetworkPartner.Should().Be(NetworkPartner);
                response.NetworkConfigStatus.Should().Be("OK");
                response.NetworkActiveStatus.Should().Be("Active");
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenNetworkPartnerIsInactive_ReturnsOkInactive()
        {
            var network = MockNetwork("I");

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(network)
                .Verifiable();

            var result = await _controller.GetNetwork(NetworkPartner);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<OkObjectResult>().Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Should().NotBeNull();
                okResult.StatusCode.Should().Be(200);

                var response = okResult.Value as NetworkResultDto;
                response.Should().NotBeNull();
                response.NetworkPartner.Should().Be(NetworkPartner);
                response.NetworkConfigStatus.Should().Be("OK");
                response.NetworkActiveStatus.Should().Be("Inactive");
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenNetworkPartnerThrowsError_ReturnsErrorUnknown()
        {
            const string ErrorMessage = "Some error";
            var expectedException = new InvalidOperationException(ErrorMessage);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ThrowsAsync(expectedException)
                .Verifiable();

            var result = await _controller.GetNetwork(NetworkPartner);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);

                var response = badResult.Value as NetworkResultDto;
                response.Should().NotBeNull();
                response.NetworkPartner.Should().Be(NetworkPartner);
                response.NetworkConfigStatus.Should().Be("Error");
                response.NetworkActiveStatus.Should().Be("Unknown");
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }
    #endregion

    #region Get Network Payor Enrollment
    public class GetNetworkPayorEnrollment : NetworkControllerTests
    {
        [Fact]
        public async Task WhenRequestHasNoNetworkPartner_ReturnsBadRequest()
        {
            const string NetworkPartner = "";
            const string ErrorMessage = "Network Partner Name Or Network Id must be provided and cannot be empty.";

            var result = await _controller.GetNetworkPayorEnrollment(NetworkPartner, ClientCode, BeCode);

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
        public async Task WhenRequestHasNoClientCode_ReturnsBadRequest()
        {
            const string ClientCode = "";
            const string ErrorMessage = "Client Code must be provided and cannot be empty.";

            var result = await _controller.GetNetworkPayorEnrollment(NetworkPartner, ClientCode, BeCode);

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
        public async Task WhenRequestHasNoEnrollmentType_ReturnsBadRequest()
        {
            const string BeCode = "";
            const string ErrorMessage = "Billing Entity Code must be provided and cannot be empty.";

            var result = await _controller.GetNetworkPayorEnrollment(NetworkPartner, ClientCode, BeCode);

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
        public async Task WhenRequestFails_ReturnsBadRequest()
        {
            const string NetworkPartner = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";
            var res = new NetworkResultDto()
            {
                NetworkPartner = NetworkPartner,
                NetworkActiveStatus = "Unknown",
                NetworkConfigStatus = "Error"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkPayorEnrollmentQuery>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var result = await _controller.GetNetworkPayorEnrollment(NetworkPartner, ClientCode, BeCode);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().BeEquivalentTo(res);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItem_ReturnsNotFound()
        {
            const string NetworkPartner = "NON-EXISTENT";

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkPayorEnrollmentQuery>(), default))
                .ReturnsAsync((NetworkPayorEnrollmentDto)null)
                .Verifiable("Send Not Called");

            var result = await _controller.GetNetworkPayorEnrollment(NetworkPartner, ClientCode, BeCode);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().BeOfType<NotFoundResult>();
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItem_ReturnsItem()
        {
            const string NetworkPartner = "GOOD-ENTRY";

            var response = new NetworkPayorEnrollmentDto
            {
                NetworkPartner = NetworkPartner,
                ClientCode = ClientCode,
                BillingEntityCode = BeCode,
                EnrollmentType = EnrollmentType,
                StartDate = DateTime.Today
            };

            var res = new NetworkResultDto()
            {
                NetworkPartner = NetworkPartner,
                NetworkActiveStatus = "Active",
                NetworkConfigStatus = "OK"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkPayorEnrollmentQuery>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.GetNetworkPayorEnrollment(NetworkPartner, ClientCode, BeCode);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Value.Should().BeOfType<NetworkResultDto>();

                var dto = okResult.Value as NetworkResultDto;
                dto.Should().NotBeNull();
                dto.NetworkPartner.Should().Be(res.NetworkPartner);
                dto.NetworkActiveStatus.Should().Be(res.NetworkActiveStatus);
                dto.NetworkConfigStatus.Should().Be(res.NetworkConfigStatus);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }
    #endregion

    #region Get Network Provider Enrollment
    public class GetNetworkProviderEnrollment : NetworkControllerTests
    {
        [Fact]
        public async Task WhenRequestHasNoNetworkPartner_ReturnsBadRequest()
        {
            const string NetworkPartner = "";
            const string ErrorMessage = "Network Partner Name Or Network Id must be provided and cannot be empty.";

            var result = await _controller.GetNetworkProviderEnrollment(NetworkPartner, ProviderTin, EnrollmentType);

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
        public async Task WhenRequestHasNoClientCode_ReturnsBadRequest()
        {
            const string ProviderTin = "";
            const string ErrorMessage = "Provider Tax Id must be provided and cannot be empty.";

            var result = await _controller.GetNetworkProviderEnrollment(NetworkPartner, ProviderTin, EnrollmentType);

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
        public async Task WhenRequestHasNoEnrollmentType_ReturnsBadRequest()
        {
            const string EnrollmentType = "";
            const string ErrorMessage = "Enrollment Type must be provided and cannot be empty.";

            var result = await _controller.GetNetworkProviderEnrollment(NetworkPartner, ProviderTin, EnrollmentType);

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
        public async Task WhenRequestFails_ReturnsBadRequest()
        {
            const string NetworkPartner = "BAD-ENTRY";
            const string ErrorMessage = "Some Error";
            var res = new NetworkResultDto()
            {
                NetworkPartner = NetworkPartner,
                NetworkActiveStatus = "Unknown",
                NetworkConfigStatus = "Error"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkProviderEnrollmentQuery>(), default))
                .ThrowsAsync(new InvalidOperationException(ErrorMessage))
                .Verifiable("Send Not Called");

            var result = await _controller.GetNetworkProviderEnrollment(NetworkPartner, ProviderTin, EnrollmentType);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
                badResult.Value.Should().BeEquivalentTo(res);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItem_ReturnsNotFound()
        {
            const string NetworkPartner = "NON-EXISTENT";

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkProviderEnrollmentQuery>(), default))
                .ReturnsAsync((NetworkProviderEnrollmentDto)null)
                .Verifiable("Send Not Called");

            var result = await _controller.GetNetworkProviderEnrollment(NetworkPartner, ProviderTin, EnrollmentType);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().BeOfType<NotFoundResult>();
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItem_ReturnsItem()
        {
            const string NetworkPartner = "GOOD-ENTRY";

            var response = new NetworkProviderEnrollmentDto
            {
                NetworkPartner = NetworkPartner,
                ProviderTin = ProviderTin,
                ProviderNpi = ProviderNpi,
                EnrollmentType = EnrollmentType,
                StartDate = DateTime.Today
            };

            var res = new NetworkResultDto()
            {
                NetworkPartner = NetworkPartner,
                NetworkActiveStatus = "Active",
                NetworkConfigStatus = "OK"
            };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkProviderEnrollmentQuery>(), default))
                .ReturnsAsync(response)
                .Verifiable("Send Not Called");

            var result = await _controller.GetNetworkProviderEnrollment(NetworkPartner, ProviderTin, EnrollmentType);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();

                var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                okResult.Value.Should().BeOfType<NetworkResultDto>();

                var dto = okResult.Value as NetworkResultDto;
                dto.Should().NotBeNull();
                dto.NetworkPartner.Should().Be(res.NetworkPartner);
                dto.NetworkActiveStatus.Should().Be(res.NetworkActiveStatus);
                dto.NetworkConfigStatus.Should().Be(res.NetworkConfigStatus);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }
    #endregion

    #region Enroll Network Providers
    public class EnrollNetworkProviders : NetworkControllerTests
    {
        [Fact]
        public async Task WhenHeaderIsNotProvided_ReturnsBadRequest()
        {
            var entity = new NetworkEnrollmentRequestDto() { Header = null };

            var result = await _controller.EnrollNetworkProviders(entity);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);

                var errorList = badResult.Value as IEnumerable<ValidationFailure>;
                errorList.Should().NotBeNull();
                errorList.Should().Contain(e => e.ErrorMessage.Contains("'Header' must"));
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenNetworkPartnerIsWrong_ReturnsBadRequest()
        {
            const int Count = 2;
            var entity = MockNetworkEnrollmentRequest(Count, "Y", "A");
            entity.Header.NetworkPartner = "NOT_" + NetworkPartner;

            var result = await _controller.EnrollNetworkProviders(entity);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var accepted = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                accepted.Should().NotBeNull();
                accepted.StatusCode.Should().Be(400);
            }
        }

        [Fact]
        public async Task WhenEnrollmentCountIsWrong_ReturnsBadRequest()
        {
            const int Count = 3;
            var entity = MockNetworkEnrollmentRequest(Count - 1, "Y", "A");
            entity.Header.Count = Count;

            var result = await _controller.EnrollNetworkProviders(entity);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);

                var resultMsg = badResult.Value as string;
                resultMsg.Should().NotBeNull();
                resultMsg.Should().StartWith($"Expected {Count}");
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestNetworkIsInvalid_ReturnsBadRequest()
        {
            const int Count = 2;

            var entity = MockNetworkEnrollmentRequest(Count, "Y", "A");
            var request = new NetworkEnrollmentRequest(entity);
            var command = new CreateNetworkEnrollmentCommand(request);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync((NetworkDto)null)
                .Verifiable();

            var result = await _controller.EnrollNetworkProviders(entity);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var accepted = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                accepted.Should().NotBeNull();
                accepted.StatusCode.Should().Be(400);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestNetworkIsInactive_ReturnsBadRequest()
        {
            const int Count = 2;

            var network = MockNetwork("I");
            var entity = MockNetworkEnrollmentRequest(Count, "Y", "A");
            var request = new NetworkEnrollmentRequest(entity);
            var command = new CreateNetworkEnrollmentCommand(request);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(network)
                .Verifiable();

            var result = await _controller.EnrollNetworkProviders(entity);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var accepted = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                accepted.Should().NotBeNull();
                accepted.StatusCode.Should().Be(400);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestThrowsError_ReturnsBadRequest()
        {
            const int Count = 2;

            var network = MockNetwork("A");
            var entity = MockNetworkEnrollmentRequest(Count, "Y", "A");
            var request = new NetworkEnrollmentRequest(entity);
            var command = new CreateNetworkEnrollmentCommand(request);
            const string ErrorMessage = "Some error";
            var expectedException = new InvalidOperationException(ErrorMessage);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(network)
                .Verifiable();

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<CreateNetworkEnrollmentCommand>(q => q.Equals(command)), default))
                .ThrowsAsync(expectedException)
                .Verifiable();

            var result = await _controller.EnrollNetworkProviders(entity);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                badResult.Should().NotBeNull();
                badResult.StatusCode.Should().Be(400);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestNotAuthenticated_ReturnsForbid()
        {
            const int Count = 2;

            var network = MockNetwork("A");
            var entity = MockNetworkEnrollmentRequest(Count, "Y", "A");
            var request = new NetworkEnrollmentRequest(entity);
            var command = new CreateNetworkEnrollmentCommand(request);
            const string ErrorMessage = "Some error";
            var expectedException = new NotAuthorizedException(ErrorMessage);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(network)
                .Verifiable();

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<CreateNetworkEnrollmentCommand>(q => q.Equals(command)), default))
                .ThrowsAsync(expectedException)
                .Verifiable();

            var result = await _controller.EnrollNetworkProviders(entity);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<ForbidResult>().Should().NotBeNull();

                var forbid = result.Result.Should().BeOfType<ForbidResult>().Subject;
                forbid.Should().NotBeNull();
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestIsValid_ReturnsAccepted()
        {
            const int Count = 2;

            var network = MockNetwork("A");
            var entity = MockNetworkEnrollmentRequest(Count, "Y", "A");
            var request = new NetworkEnrollmentRequest(entity);
            var command = new CreateNetworkEnrollmentCommand(request);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(network)
                .Verifiable();

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<CreateNetworkEnrollmentCommand>(q => q.Equals(command)), default))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _controller.EnrollNetworkProviders(entity);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<AcceptedResult>().Should().NotBeNull();

                var accepted = result.Result.Should().BeOfType<AcceptedResult>().Subject;
                accepted.Should().NotBeNull();
                accepted.StatusCode.Should().Be(202);

                var response = accepted.Value as NetworkEnrollmentResponse;
                response.Should().NotBeNull();
                response.CorrelationId.Should().Be(CorrelationId);
                response.NetworkPartner.Should().Be(NetworkPartner);
                response.EnrollmentCount.Should().Be(Count);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }
    #endregion

    #region Helpers
    protected static NetworkDto MockNetwork(string active = "A")
    {
        return new NetworkDto()
        {
            NetworkId = 1,
            NetworkPartner = NetworkPartner,
            NetworkType = "STP",
            ActiveStatus = active
        };
    }

    protected static NetworkEnrollmentRequestDto MockNetworkEnrollmentRequest(int count, string enabled, string active)
    {
        var providers = new List<NetworkEnrollmentListDto>();
        for (int i = 0; i < count; i++)
        {
            providers.Add(new NetworkEnrollmentListDto()
            {
                ProviderTin = $"{ProviderTin.Substring(1, 8)}{count}",
                LegalEntityName = ProviderName,
                Dba = ProviderDba,
                EnrollmentList = new List<NetworkEnrollmentItemDto>()
                {
                    new()
                    {
                        Npi = ProviderNpi,
                        Address1 = "123 SOME ST",
                        Address2 = "",
                        City = "Plano",
                        State = "TX",
                        Zip9 = "75000",
                        EnrollmentType = EnrollmentType,
                        EnrollmentEligibilityStatus = enabled,
                        EffectiveDate = DateTime.Today,
                        InitialEnrollmentDate = DateTime.Today,
                        LastUpdatedUtcDate = DateTime.Today,
                        EnrollmentStatusCd = active,
                        EnrollmentStatusReason = "Fraud"
                    }
                }
            });
        }

        return new NetworkEnrollmentRequestDto()
        {
            Header = new NetworkEnrollmentHeaderDto()
            {
                NetworkPartner = NetworkPartner,
                CorrelationId = CorrelationId,
                Count = count
            },
            PartnerProviders = providers
        };
    }
    #endregion
}
