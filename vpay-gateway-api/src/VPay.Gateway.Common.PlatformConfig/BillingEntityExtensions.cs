namespace VPay.Gateway.Common.PlatformConfig;

public static class BillingEntityExtensions
{
    public static bool IsWarranty(this BillingEntityDto? billingEntity)
    {
        if (billingEntity == null)
        {
            return false;
        }

        if (billingEntity.Type == null)
        {
            return false;
        }

        return string.Equals(billingEntity.Type.Name, "WARRANTY", System.StringComparison.InvariantCultureIgnoreCase);
    }
}
