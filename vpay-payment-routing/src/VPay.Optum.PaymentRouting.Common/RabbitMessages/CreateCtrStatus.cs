using System;

namespace VPay.Optum
{
    /// <summary>
    /// Note that the namespace/signature of this class is needed by MassTransit to
    /// properly route/build the wrapper for.  
    /// </summary>
    public record CreateCtrStatus(long TransactionId, string Status, DateTime Timestamp);
}
