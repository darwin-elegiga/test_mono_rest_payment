using System;
using VPay.DocSys.Common;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Common.Edi835
{
    /// <summary>
    /// When unable to locate the legacy data in the Portal835Data replicated data. 
    /// </summary>
    [Serializable]
    public class Portal835DataNotFoundException : System.IO.FileNotFoundException
    {
        public DS.Edi835 Edi835 { get; }

        public Portal835DataNotFoundException(DS.Edi835 edi835) : base()
        {
            Edi835 = edi835;
        }
        public Portal835DataNotFoundException(string message, DS.Edi835 edi835) : base(message)
        {
            Edi835 = edi835;
        }
        public Portal835DataNotFoundException(string message, DS.Edi835 edi835, Exception innerException) : base(message, innerException)
        {
            Edi835 = edi835;
        }
        public Portal835DataNotFoundException(string message, DS.Edi835 edi835, string fileName) : base(message, fileName)
        {
            Edi835 = edi835;
        }
        public Portal835DataNotFoundException(string message, DS.Edi835 edi835, string fileName, Exception innerException) : base(message, fileName, innerException)
        {
            Edi835 = edi835;
        }
    }
}
