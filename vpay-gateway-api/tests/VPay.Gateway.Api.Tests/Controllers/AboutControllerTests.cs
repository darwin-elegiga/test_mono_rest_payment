using VPay.Gateway.Api;
using VPay.Gateway.Api.Controllers;
using Xunit;

namespace VPay.Gateway.Management.Api.Tests.Controllers;

/// <summary>
/// Tests for <see cref="AboutController"/>
/// </summary>
public class AboutControllerTests
{
    [Fact]
    public void WhenCalled_OkReturnedWithAboutInfoData()
    {
        var controller = new AboutController();
        var aboutInfo = controller.GetDetail();

        Assert.NotNull(aboutInfo);
        Assert.Equivalent(AboutInfo.GetBuildAboutInfo(), aboutInfo);
    }
}
