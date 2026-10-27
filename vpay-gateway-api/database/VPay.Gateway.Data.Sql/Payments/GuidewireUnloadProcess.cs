namespace VPay.Gateway.Data.Sql.Payments;

/// <summary>
/// This model represents the GuidewireUnloadProcess table in the database.
/// </summary>
public record GuidewireUnloadProcess(
    int Id,
    string ReasonCode,
    GuidewireUnloadProcessType ProcessType,
    string Comment);
