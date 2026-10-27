using FluentAssertions;
using Moq;
using VPay.Gateway.Common.Enrollments.Models;
using Xunit;

namespace VPay.Gateway.Common.Enrollments.Tests;

/// <summary>
/// Tests for the <see cref="HasActiveEnrollmentQueryHandler"/>
/// </summary>
public class HasActiveEnrollmentQueryHandlerTests : IDisposable
{
    private readonly Mock<IEnrollmentsApi> _apiMock = new(MockBehavior.Strict);
    private readonly HasActiveEnrollmentQueryHandler _handler;

    protected HasActiveEnrollmentQueryHandlerTests() =>
        _handler = new HasActiveEnrollmentQueryHandler(_apiMock.Object);

    public void Dispose() =>
        _apiMock.VerifyAll();

    public class Constructor : HasActiveEnrollmentQueryHandlerTests
    {
        [Fact]
        public void WhenEnrollmentsApiIsNull_ThrowsArgumentNullException() =>
            new Action(() => new HasActiveEnrollmentQueryHandler(null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("enrollmentsApi");
    }

    public class Handle : HasActiveEnrollmentQueryHandlerTests
    {
        private const string ClientCode = "CBC";
        private const string BillingEntityCode = "CBC0001001";
        private const string MemberId = "cc:1234";

        /* NOTE: There was a change in the response from the BPM -> MEP api where the casing on the Member ID
         * could be different than what was passed to the API.  We want to make sure we can still find the
         * member, so we'll test a few different casings
        */
        [Theory]
        [InlineData(MemberId)]
        [InlineData("CC:1234")]
        [InlineData("cC:1234")]
        public async Task WhenNoEnrollments_ReturnsFalse(string memberId)
        {
            SetupApiMock(memberId, Enumerable.Empty<dynamic>());

            var query = new HasActiveEnrollmentQuery(ClientCode, BillingEntityCode, MemberId);
            var result = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task WhenActiveEnrollmentsExist_ReturnsTrue()
        {
            SetupApiMock(MemberId, new dynamic[] { new object() });

            var query = new HasActiveEnrollmentQuery(ClientCode, BillingEntityCode, MemberId);
            var result = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            result.Should().BeTrue();
        }

        private void SetupApiMock(string memberId, IEnumerable<dynamic> results) =>
            _apiMock
                .Setup(
                    x => x.GetActiveEnrollments(
                        It.Is<BatchExistingClaimsRequest>(
                            r => r.ClientCode == ClientCode
                                 && r.MemberIds.SequenceEqual(new[] { MemberId }, StringComparer.OrdinalIgnoreCase)),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    // NOTE: We want to deserialize this here to verify that everything works correctly from the API.
                    // We had an issue (CP-24630) where the casing of the Member ID was different in the response,
                    // and want to make sure everything still works correctly
                    Newtonsoft.Json.JsonConvert.DeserializeObject<BatchExistingClaimsResponse>(
                        $@"{{
                          ""results"": {{
                            ""{memberId}"": [ {(results.Any() ? "{}" : "")} ]
                          }}
                        }}",
                        new Newtonsoft.Json.JsonSerializerSettings
                        {
                            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                        }))
                .Verifiable();
    }
}
