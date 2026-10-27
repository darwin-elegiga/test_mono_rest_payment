namespace VPay.Gateway.Common.Enrollments;

[Serializable]
public class EnrollmentInProgressException : Exception
{
    public EnrollmentInProgressException(string clientCode, string memberId)
    {
        ClientCode = clientCode;
        MemberId = memberId;
    }

    public EnrollmentInProgressException(string clientCode, string memberId, string message) : base(message)
    {
        ClientCode = clientCode;
        MemberId = memberId;
    }

    public EnrollmentInProgressException(string clientCode, string memberId, string message, Exception inner) : base(message, inner)
    {
        ClientCode = clientCode;
        MemberId = memberId;
    }

    /// <summary>
    /// Gets the client code associated with the enrollment that is active.
    /// </summary>
    public string ClientCode { get; }

    /// <summary>
    /// Gets the member id associated with the enrollment that is active.
    /// </summary>
    public string MemberId { get; }
}
