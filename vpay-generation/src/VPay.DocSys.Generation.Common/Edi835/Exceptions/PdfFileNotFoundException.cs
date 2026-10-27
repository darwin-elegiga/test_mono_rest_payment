using System;
using VPay.DocSys.Common;
using DS=VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Common.Edi835
{
    /// <summary>
    /// When unable to locate the .pdf file, this custom exception will allow for easier alerting/tracking
    /// from GrayLog or other monitoring solutions, since in production, this will be a very big deal.
    /// Make sure to include, as inner exception and/or message, any of the "reasons" why the pdf could not be found.
    /// (ie. unable to locate archive (exception), not found within the archive, etc)
    /// </summary>
    /// <remarks>While I realize it is not necessary to implement all of the constructors unless I am
    /// explicitly using them, I feel it is good practice to do so, to avoid having to refactor the code
    /// at some later point to include them. ( Y-are-GNI, mateys! )</remarks>
    [Serializable]
    public class PdfFileNotFoundException : System.IO.FileNotFoundException
    {
        public DS.Edi835 Edi835 { get; }

        public PdfFileNotFoundException(DS.Edi835 edi835) : base()
        {
            Edi835 = edi835;
        }
        public PdfFileNotFoundException(string message, DS.Edi835 edi835) : base(message)
        {
            Edi835 = edi835;
        }
        public PdfFileNotFoundException(string message, DS.Edi835 edi835, Exception innerException) : base(message, innerException)
        {
            Edi835 = edi835;
        }
        public PdfFileNotFoundException(string message, DS.Edi835 edi835, string fileName) : base(message, fileName)
        {
            Edi835 = edi835;
        }
        public PdfFileNotFoundException(string message, DS.Edi835 edi835, string fileName, Exception innerException) : base(message, fileName, innerException)
        {
            Edi835 = edi835;
        }
    }
}
