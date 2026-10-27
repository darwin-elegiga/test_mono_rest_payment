using VPay.DocSys.Common;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Common.Helpers
{
    public static class VPageDocumentTypeHelper
    {
        public static string ToRedCardFileExtension(this VPaygeDocumentType vPaygeDocumentType, bool isReissue)
        {
            switch (vPaygeDocumentType)
            {
                case VPaygeDocumentType.VCard_Masked:
                    return Constants.FileExtensions.PacketVCardMaskedPdf.Replace("_eob", "_cover");

                case VPaygeDocumentType.VCard_Clear:
                    return Constants.FileExtensions.PacketVCardClearPdf.Replace("_eob", "_cover");

                case VPaygeDocumentType.EFT:
                case VPaygeDocumentType.SDA:
                case VPaygeDocumentType.OptumAch:
                    return Constants.FileExtensions.PacketEftPdf.Replace("_eob", "");

                case VPaygeDocumentType.NoPay:
                    return Constants.FileExtensions.PacketNoPayPdf.Replace("_eob", "");

                default:
                    return Constants.FileExtensions.PacketCheckPdf.Replace("_eob", isReissue ? "_cover" : "");
            }
        }
    }
}
