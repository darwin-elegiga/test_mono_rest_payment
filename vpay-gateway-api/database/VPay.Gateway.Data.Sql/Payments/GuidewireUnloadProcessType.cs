namespace VPay.Gateway.Data.Sql.Payments;

public enum GuidewireUnloadProcessType
{
    DoNothing = 0,
    Cleared = 1,
    UnloadReissue = 2,
    UnloadSendback = 3
}
