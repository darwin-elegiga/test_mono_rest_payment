using System.Runtime.Serialization;

namespace VPay.Gateway.Common.Preferencing.Enums;

public enum NetworkEnrollmentEnabledStatus
{
    [EnumMember(Value = "")]
    None,
    [EnumMember(Value = "N")]
    Disabled,
    [EnumMember(Value = "Y")]
    Enabled
}
