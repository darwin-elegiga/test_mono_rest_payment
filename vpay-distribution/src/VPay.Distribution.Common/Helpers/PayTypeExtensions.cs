using VPay.DocSys.Common;
using VPay.DocSys.Storage;

namespace VPay.Distribution.Common.Helpers
{
    public static class PayTypeExtensions
    {
        public static string ToFileExtension(string payType)
        {
            switch (payType)
            {
                case PayType.CLCHK:
                case PayType.CLCCK:
                    return Constants.FileExtensions.PacketCheckPdf;

                case PayType.CLVCP:
                case "CLCCP":
                    return Constants.FileExtensions.PacketVCardClearPdf;

                case PayType.CLEFT:
                case PayType.CLCFT:
                case PayType.CLSDA:
                case PayType.OPACH:
                    return Constants.FileExtensions.PacketEftPdf;

                case PayType.CLNPF:
                    return Constants.FileExtensions.PacketNoPayPdf;

                default:
                    return "unknown.pdf.doesnotexist";
            }
        }
    }
}
