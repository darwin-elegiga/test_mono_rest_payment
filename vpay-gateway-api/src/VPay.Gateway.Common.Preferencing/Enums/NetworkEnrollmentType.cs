using System.Runtime.Serialization;

namespace VPay.Gateway.Common.Preferencing.Enums;

public enum NetworkEnrollmentType
{
    [EnumMember(Value = "")]
    None,
    [EnumMember(Value = "B2B")]
    B2B,
    [EnumMember(Value = "C2B")]
    C2B
}
