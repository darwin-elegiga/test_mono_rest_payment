namespace VPay.Gateway.Management.Api.Models;

public class GuidewireNoteDto
{
    public string Subject { get; set; }

    public NoteTopic? Topic { get; set; }

    public string Body { get; set; }

    public enum NoteTopic
    {
        General,
        FNOL,
        Coverage,
        Investigation,
        Medical,
        Evaluation,
        Settlement,
        Subrogation,
        Salvage,
        Litigation,
        Denial,
        Reinsurance
    }
}
