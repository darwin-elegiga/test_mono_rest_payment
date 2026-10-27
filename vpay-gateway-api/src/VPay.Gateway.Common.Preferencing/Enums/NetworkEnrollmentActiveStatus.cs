using System.Runtime.Serialization;

namespace VPay.Gateway.Common.Preferencing.Enums;

public enum NetworkEnrollmentActiveStatus
{
    [EnumMember(Value = "")]
    None,
    [EnumMember(Value = "I")]
    Inactive,
    [EnumMember(Value = "A")]
    Active
}
