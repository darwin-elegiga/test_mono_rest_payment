using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Gateway.Management.Api.Controllers;
using Xunit;

namespace VPay.Gateway.Management.Api.Tests.Controllers;

/// <summary>
/// Tests for <see cref="PaymentsController"/>
/// </summary>
public class PaymentsControllerTests
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly PaymentsController _controller;
    private readonly ILoggerFactory _loggerFactory = new LoggerFactory();
    public PaymentsControllerTests()
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
        }, _loggerFactory);
        _mapper = configuration.CreateMapper();
        _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
        _controller = new PaymentsController(_mediator, _mapper);
    }

    public class Constructor : PaymentsControllerTests
    {
        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new PaymentsController(null!, _mapper);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mediator");
        }

        [Fact]
        public void WhenMapperIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new PaymentsController(_mediator, null!);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mapper");
        }
    }
}
