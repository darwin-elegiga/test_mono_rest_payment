using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using VPay.Gateway.Identity;
using Xunit;

namespace VPay.Gateway.Api.Tests;

public class UserInfoTests
{
    private readonly IHttpContextAccessor _accessor;
    public UserInfoTests()
    {
        var session = Substitute.For<ISession>();
        var httpRequest = Substitute.For<HttpRequest>();
        var httpResponse = Substitute.For<HttpResponse>();

        _accessor = Substitute.For<IHttpContextAccessor>();

        _accessor.HttpContext.Session.Returns(session);
        _accessor.HttpContext.Request.Returns(httpRequest);
        _accessor.HttpContext.Response.Returns(httpResponse);
    }

    [Fact]
    public void CanAccess_WhenNotAuthenticated_ReturnsFalse()
    {
        _accessor.HttpContext.User.Identity.IsAuthenticated.Returns(false);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.CanAccess("ABC", "ABC0001001");
        Assert.False(result);
    }

    [Fact]
    public void CanAccess_WhenAuthenticatedAndClientValid_ReturnsTrue()
    {
        _accessor.HttpContext.User.Identity.IsAuthenticated.Returns(true);

        var clientCodes = new[] { new Claim(ClaimNames.ClientCode, "ABC") };
        var beCodes = new[] { new Claim(ClaimNames.BillingEntityCode, "") };
        var beCodeExclusions = new[] { new Claim(ClaimNames.BillingEntityCode, "") };

        _accessor.HttpContext.User.FindAll(ClaimNames.ClientCode).Returns(clientCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityCode).Returns(beCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityExclusion).Returns(beCodeExclusions);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.CanAccess("ABC", "ABC0001001");
        Assert.True(result);
    }

    [Fact]
    public void CanAccess_WhenAuthenticatedAndClientValidAndBillingValid_ReturnsTrue()
    {
        _accessor.HttpContext.User.Identity.IsAuthenticated.Returns(true);

        var clientCodes = new[] { new Claim(ClaimNames.ClientCode, "ABC") };
        var beCodes = new[] { new Claim(ClaimNames.BillingEntityCode, "ABC0001001") };
        var beCodeExclusions = new[] { new Claim(ClaimNames.BillingEntityCode, "") };

        _accessor.HttpContext.User.FindAll(ClaimNames.ClientCode).Returns(clientCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityCode).Returns(beCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityExclusion).Returns(beCodeExclusions);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.CanAccess("ABC", "ABC0001001");
        Assert.True(result);
    }

    [Fact]
    public void CanAccess_WhenAuthenticatedAndClientValidAndBillingExcluded_ReturnsFalse()
    {
        _accessor.HttpContext.User.Identity.IsAuthenticated.Returns(true);

        var clientCodes = new[] { new Claim(ClaimNames.ClientCode, "ABC") };
        var beCodes = new[] { new Claim(ClaimNames.BillingEntityCode, "") };
        var beCodeExclusions = new[] { new Claim(ClaimNames.BillingEntityCode, "ABC0001001") };

        _accessor.HttpContext.User.FindAll(ClaimNames.ClientCode).Returns(clientCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityCode).Returns(beCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityExclusion).Returns(beCodeExclusions);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.CanAccess("ABC", "ABC0001001");
        Assert.False(result);
    }

    [Theory]
    [InlineData("OPACH")]
    [InlineData("CLCFT")]
    [InlineData("CLNPF")]
    public void CanAccess_WhenAuthenticatedAndRoleRestrictedAndPaymentTypeAllowed_ReturnsTrue(string paymentType)
    {
        _accessor.HttpContext.User.Identity.IsAuthenticated.Returns(true);
        _accessor.HttpContext.User.IsInRole(UserInfo.PaymentTypeRestrictedRoleName).Returns(true);

        var clientCodes = new[] { new Claim(ClaimNames.ClientCode, "ABC") };
        var beCodes = new[] { new Claim(ClaimNames.BillingEntityCode, "ABC0001001") };
        var beCodeExclusions = new[] { new Claim(ClaimNames.BillingEntityCode, "") };

        _accessor.HttpContext.User.FindAll(ClaimNames.ClientCode).Returns(clientCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityCode).Returns(beCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityExclusion).Returns(beCodeExclusions);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.CanAccess("ABC", "ABC0001001", paymentType);
        Assert.True(result);
    }

    [Theory]
    [InlineData("CLVCP")]
    [InlineData("CLCHK")]
    [InlineData("CLSDA")]
    public void CanAccess_WhenAuthenticatedAndRoleRestrictedAndPaymentTypeNotAllowed_ReturnsFalse(string paymentType)
    {
        _accessor.HttpContext.User.Identity.IsAuthenticated.Returns(true);
        _accessor.HttpContext.User.IsInRole(UserInfo.PaymentTypeRestrictedRoleName).Returns(true);

        var clientCodes = new[] { new Claim(ClaimNames.ClientCode, "ABC") };
        var beCodes = new[] { new Claim(ClaimNames.BillingEntityCode, "ABC0001001") };
        var beCodeExclusions = new[] { new Claim(ClaimNames.BillingEntityCode, "") };

        _accessor.HttpContext.User.FindAll(ClaimNames.ClientCode).Returns(clientCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityCode).Returns(beCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityExclusion).Returns(beCodeExclusions);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.CanAccess("ABC", "ABC0001001", paymentType);
        Assert.False(result);
    }

    [Theory]
    [InlineData("OPACH")]
    [InlineData("CLCFT")]
    [InlineData("CLNPF")]
    public void CanAccess_WhenAuthenticatedAndRoleNotRestrictedAndPaymentTypeAllowed_ReturnsTrue(string paymentType)
    {
        _accessor.HttpContext.User.Identity.IsAuthenticated.Returns(true);
        _accessor.HttpContext.User.IsInRole(UserInfo.PaymentTypeRestrictedRoleName).Returns(false);

        var clientCodes = new[] { new Claim(ClaimNames.ClientCode, "ABC") };
        var beCodes = new[] { new Claim(ClaimNames.BillingEntityCode, "ABC0001001") };
        var beCodeExclusions = new[] { new Claim(ClaimNames.BillingEntityCode, "") };

        _accessor.HttpContext.User.FindAll(ClaimNames.ClientCode).Returns(clientCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityCode).Returns(beCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityExclusion).Returns(beCodeExclusions);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.CanAccess("ABC", "ABC0001001", paymentType);
        Assert.True(result);
    }

    [Theory]
    [InlineData("CLVCP")]
    [InlineData("CLCHK")]
    [InlineData("CLSDA")]
    public void CanAccess_WhenAuthenticatedAndRoleNotRestrictedAndPaymentTypeNotAllowed_ReturnsTrue(string paymentType)
    {
        _accessor.HttpContext.User.Identity.IsAuthenticated.Returns(true);
        _accessor.HttpContext.User.IsInRole(UserInfo.PaymentTypeRestrictedRoleName).Returns(false);

        var clientCodes = new[] { new Claim(ClaimNames.ClientCode, "ABC") };
        var beCodes = new[] { new Claim(ClaimNames.BillingEntityCode, "ABC0001001") };
        var beCodeExclusions = new[] { new Claim(ClaimNames.BillingEntityCode, "") };

        _accessor.HttpContext.User.FindAll(ClaimNames.ClientCode).Returns(clientCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityCode).Returns(beCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityExclusion).Returns(beCodeExclusions);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.CanAccess("ABC", "ABC0001001", paymentType);
        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CanAccess_WhenAuthenticatedAndRoleNotRestrictedAndPaymentTypeNotProvided_ReturnsTrue(string paymentType)
    {
        _accessor.HttpContext.User.Identity.IsAuthenticated.Returns(true);
        _accessor.HttpContext.User.IsInRole(UserInfo.PaymentTypeRestrictedRoleName).Returns(false);

        var clientCodes = new[] { new Claim(ClaimNames.ClientCode, "ABC") };
        var beCodes = new[] { new Claim(ClaimNames.BillingEntityCode, "ABC0001001") };
        var beCodeExclusions = new[] { new Claim(ClaimNames.BillingEntityCode, "") };

        _accessor.HttpContext.User.FindAll(ClaimNames.ClientCode).Returns(clientCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityCode).Returns(beCodes);
        _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityExclusion).Returns(beCodeExclusions);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.CanAccess("ABC", "ABC0001001", paymentType);
        Assert.True(result);
    }

    [Fact]
    public void IsInRole_WhenUserIsNotInRole_ReturnsFalse()
    {
        const string RoleName = "SOME_ROLE";
        _accessor.HttpContext.User.IsInRole(UserInfo.PaymentTypeRestrictedRoleName).Returns(false);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.IsInRole(RoleName);
        Assert.False(result);
    }

    [Fact]
    public void IsInRole_WhenUserIsInRole_ReturnsTrue()
    {
        const string RoleName = "SOME_ROLE";
        _accessor.HttpContext.User.IsInRole(RoleName).Returns(true);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.IsInRole(RoleName);
        Assert.True(result);
    }

    [Fact]
    public void IsInRoleWithRestrictedPaymentTypes_WhenRoleIsNotRestricted_ReturnsFalse()
    {
        _accessor.HttpContext.User.IsInRole(UserInfo.PaymentTypeRestrictedRoleName).Returns(false);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.IsInRoleWithRestrictedPaymentTypes();
        Assert.False(result);
    }

    [Fact]
    public void IsInRoleWithRestrictedPaymentTypes_WhenRoleIsRestricted_ReturnsTrue()
    {
        _accessor.HttpContext.User.IsInRole(UserInfo.PaymentTypeRestrictedRoleName).Returns(true);

        var userInfo = new UserInfo(_accessor);
        Assert.NotNull(userInfo);

        var result = userInfo.IsInRoleWithRestrictedPaymentTypes();
        Assert.True(result);
    }
}
