using System.Runtime.Serialization;

namespace VPay.Gateway.Common.Preferencing.Enums;

public enum NetworkEnrollmentReasonCode
{
    [EnumMember(Value = "")]
    None,
    [EnumMember(Value = "ZBA0")]
    ZeroBalanceAccount,
    [EnumMember(Value = "UTP0")]
    UnableToPost,
    [EnumMember(Value = "ITPO")]
    InvalidProvider,
    [EnumMember(Value = "UTLA")]
    UnableToLocateAccount,
    [EnumMember(Value = "DPCB")]
    DuplicatePaymentCreatesCreditBalance,
    [EnumMember(Value = "ACOC")]
    AccountChargeOffSentToCollection,
    [EnumMember(Value = "CNGR")]
    CouldNotAllocateToGuarantorRelationship
}
