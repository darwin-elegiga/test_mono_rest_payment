using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace VPay.Gateway.Common.Enrollments.Tests;

/// <summary>
/// Tests for the <see cref="EnrollmentInProgressException"/>
/// </summary>
public class EnrollmentInProgressExceptionTests
{
    private const string ClientCode = "CBC";
    private const string MemberId = "cc:1234";
    private const string Message = "Some error message";

    /// <summary>
    /// Test the various versions of the constructor.
    /// </summary>
    public class Constructor : EnrollmentInProgressExceptionTests
    {
        [Fact]
        public void WithClientAndMember_SetsProperties()
        {
            using var _ = new AssertionScope();
            VerifyCustomProperties(new EnrollmentInProgressException(ClientCode, MemberId));
        }

        [Fact]
        public void WithClientAndMemberAndMessage_SetsProperties()
        {
            var exception = new EnrollmentInProgressException(ClientCode, MemberId, Message);
            using var _ = new AssertionScope();
            VerifyCustomProperties(exception);
            exception.Message.Should().Be(Message);
        }

        [Fact]
        public void WithClientAndMemberAndMessageAndInnerException_SetsProperties()
        {
            var innerException = new Exception("Testing");
            var exception = new EnrollmentInProgressException(ClientCode, MemberId, Message, innerException);
            using var _ = new AssertionScope();
            VerifyCustomProperties(exception);
            exception.Message.Should().Be(Message);
            exception.InnerException.Should().BeSameAs(innerException);
        }

        private static void VerifyCustomProperties(EnrollmentInProgressException exception)
        {
            exception.ClientCode.Should().Be(ClientCode);
            exception.MemberId.Should().Be(MemberId);
        }
    }
}
