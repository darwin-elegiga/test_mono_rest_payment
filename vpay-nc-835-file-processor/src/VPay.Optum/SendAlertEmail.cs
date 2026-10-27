namespace VPay.Optum
{
    public record SendAlertEmail(string Message, string Subject, bool IsHtml = true);
}
