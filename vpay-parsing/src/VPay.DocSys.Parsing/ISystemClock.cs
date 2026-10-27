using System;

namespace VPay.DocSys.Parsing;

public interface ISystemClock
{
    /// <summary>
    /// Retrieves the current system time
    /// </summary>
    DateTime Now { get; }
}
