using Microsoft.AspNetCore.Mvc;

namespace VPay.Gateway.Api.Controllers;

[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public sealed class AboutController : ControllerBase
{
    /// <summary>
    /// About the build that created the service
    /// </summary>
    /// <returns>Information about the build that created the service</returns>
    /// <response code="200">Information about the build that created the service</response>
    [HttpGet]
    [ProducesResponseType(typeof(AboutInfo), StatusCodes.Status200OK)]
    public AboutInfo GetDetail() =>
        AboutInfo.GetBuildAboutInfo();
}
