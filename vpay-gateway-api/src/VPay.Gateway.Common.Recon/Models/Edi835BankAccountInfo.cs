namespace VPay.Gateway.Common.Recon.Models;

public class Edi835BankAccountInfo
{
    public BankAccountInfoDto? FromBankInfo { get; set; }

    public BankAccountInfoDto? ToBankInfo { get; set; }
}
