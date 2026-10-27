using System;

namespace VPay.DocSys.Parsing.Common
{
    public class TarFileRecord
    {
        public Guid Guid { get; set; }
        public string FileName { get; set; }
        public string TarRecordName { get; set; }
        public byte[] Data { get; set; }
    }
}
