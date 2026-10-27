namespace VPay.Gateway.ClientProcessor.Rabbit;

public class ProcessEnrollmentStatusRecords
{
    public ProcessEnrollmentStatusRecords(string? clientCode, string? memberId)
    {
        ClientCode = clientCode;
        MemberId = memberId;
    }

    public string? ClientCode { get; }

    public string? MemberId { get; }
}
