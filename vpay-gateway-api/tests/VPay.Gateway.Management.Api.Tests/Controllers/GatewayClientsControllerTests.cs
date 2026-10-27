using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Gateway.Management.Api.Controllers;
using Xunit;

namespace VPay.Gateway.Management.Api.Tests.Controllers;

/// <summary>
/// Tests for <see cref="GatewayClientsController"/>
/// </summary>
public class GatewayClientsControllerTests
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly GatewayClientsController _controller;
    private readonly ILoggerFactory _loggerFactory = new LoggerFactory();
    public GatewayClientsControllerTests()
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
        _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
        _controller = new GatewayClientsController(_mediator, _mapper);
    }

    public class Constructor : GatewayClientsControllerTests
    {
        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new GatewayClientsController(null!, _mapper);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mediator");
        }

        [Fact]
        public void WhenMapperIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new GatewayClientsController(_mediator, null!);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mapper");
        }
    }
}
