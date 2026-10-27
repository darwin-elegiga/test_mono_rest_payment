using System.Runtime.Serialization;

namespace VPay.Gateway.Common.Preferencing.Enums;

public enum NetworkPaymentRejectionCode
{
    [EnumMember(Value = "")]
    None,
    [EnumMember(Value = "ZBA0")]
    ZeroBalanceAccount,
    [EnumMember(Value = "UTP0")]
    UnableToPost,
    [EnumMember(Value = "ITP0")]
    InvalidProvider,
    [EnumMember(Value = "UTLA")]
    UnableToLocateAccount,
    [EnumMember(Value = "DPCB")]
    DuplicatePaymentCreatesCreditBalance,
    [EnumMember(Value = "ACOC")]
    AccountChargeOffSentToCollection,
    [EnumMember(Value = "CNGR")]
    CouldNotAllocateToGuarantorRelationship,
    [EnumMember(Value = "RRJD")]
    RemittanceAdviceRejectedByPartner
}
