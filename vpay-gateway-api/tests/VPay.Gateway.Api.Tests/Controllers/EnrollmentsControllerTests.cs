using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Gateway.Api.Controllers;
using VPay.Gateway.Application;
using VPay.Gateway.Common.PlatformConfig;
using Xunit;

namespace VPay.Gateway.Api.Tests.Controllers;

/// <summary>
/// Tests for <see cref="EnrollmentsController"/>
/// </summary>
public class EnrollmentsControllerTests
{
    private readonly IUserInfo _userInfo;
    private readonly IMediator _mediator;
    private readonly IPlatformConfigApi _platformConfigApi;
    private readonly ILogger<EnrollmentsController> _logger;
    private readonly EnrollmentsController _controller;

    public EnrollmentsControllerTests()
    {
        _userInfo = Mock.Of<IUserInfo>(MockBehavior.Strict);
        _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
        _platformConfigApi = Mock.Of<IPlatformConfigApi>(MockBehavior.Strict);
        _logger = new NullLogger<EnrollmentsController>();
        _controller = new EnrollmentsController(_userInfo, _mediator, _platformConfigApi, _logger);
    }

    public class Constructor : EnrollmentsControllerTests
    {
        [Fact]
        public void WhenUserInfoIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new EnrollmentsController(null!, _mediator, _platformConfigApi, _logger);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("userInfo");
        }

        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new EnrollmentsController(_userInfo, null!, _platformConfigApi, _logger);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mediator");
        }

        [Fact]
        public void WhenPlatformConfigIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new EnrollmentsController(_userInfo, _mediator, null!, _logger);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("platformConfigApi");
        }

        [Fact]
        public void WhenLoggerIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new EnrollmentsController(_userInfo, _mediator, _platformConfigApi, null!);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("logger");
        }
    }
}
