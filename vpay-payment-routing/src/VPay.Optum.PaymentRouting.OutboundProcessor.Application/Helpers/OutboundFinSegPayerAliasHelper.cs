namespace VPay.Optum.PaymentRouting.OutboundProcessor.Application.Helpers
{
    public static class OutboundFinSegPayerAliasHelper
    {
        /// <summary>
        /// When Optum receives files from VPay, they have already use MUM25 and DUM25 internally to send to UMR,
        /// as such, when they get files from VPay, they have special logic that filters on MUMV5/DUMV5, and
        /// puts them in the correct sequence from their system to send to UMR.  ONLY use this on the output of files
        /// </summary>
        public static string OutboundFinSegPayerAliasConverter(this string payerAlias)
        {
            return payerAlias switch
            {
                "MUM25" => "MUMV5",
                "DUM25" => "DUMV5",
                _ => payerAlias
            };
        }
    }
}
