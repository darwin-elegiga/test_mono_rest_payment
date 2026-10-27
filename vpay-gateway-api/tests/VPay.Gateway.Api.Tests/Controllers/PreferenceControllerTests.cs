using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VPay.Gateway.Api.Controllers;

namespace VPay.Gateway.Api.Tests.Controllers;
public partial class PreferenceControllerTests
{
    private readonly PreferencesController _sut;
    private readonly MediatR.IMediator _mockMediator = Substitute.For<MediatR.IMediator>();
    private readonly ILogger<PreferencesController> _nullLogger = NullLogger<PreferencesController>.Instance;

    public PreferenceControllerTests()
    {
        _sut = new PreferencesController(
            _mockMediator,
            _nullLogger);
        var httpRequest = Substitute.For<HttpRequest>();
        var session = Substitute.For<ISession>();
        var httpResponse = Substitute.For<HttpResponse>();
        var httpContext = Substitute.For<IHttpContextAccessor>();
        httpContext.HttpContext.Request.Returns(httpRequest);
        httpContext.HttpContext.Session.Returns(session);
        httpContext.HttpContext.Response.Returns(httpResponse);
        _sut.HttpContext.Returns(httpContext.HttpContext);
    }
}
