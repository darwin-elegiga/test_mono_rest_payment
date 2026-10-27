using VPay.DocSys.Common;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Parsing.Common.Extensions
{
    public static class PayTypeHelper
    {
        public static string ToRedCardFileExtension(this string payType, bool isReissue)
        {
            ArgumentHelpers.ThrowIf.IsNull(payType, nameof(payType));

            switch (payType.ToUpper())
            {
                case PayType.CLVCP:
                    return Constants.FileExtensions.PacketVCardClearPdf.Replace("_eob", "_cover");

                case PayType.CLEFT:
                case PayType.CLSDA:
                case PayType.OPACH:
                    return Constants.FileExtensions.PacketEftPdf.Replace("_eob", "");

                case PayType.CLNPF:
                    return Constants.FileExtensions.PacketNoPayPdf.Replace("_eob", "");

                default:
                    return Constants.FileExtensions.PacketCheckPdf.Replace("_eob", isReissue ? "_cover" : "");
            }
        }
    }
}
