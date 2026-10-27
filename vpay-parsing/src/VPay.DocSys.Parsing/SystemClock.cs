using System;
using System.Diagnostics.CodeAnalysis;

namespace VPay.DocSys.Parsing;

[ExcludeFromCodeCoverage]
public class SystemClock : ISystemClock
{
    public DateTime Now => DateTime.Now;
}
