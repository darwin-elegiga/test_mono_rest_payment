using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VPay.DocSys.Common;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Parsing
{
    public class Inbound835RemittanceAdviceGeneratedFileStream : IGeneratedFileStream
    {
        public Stream Stream { get; set; }
        public long Length { get; set; }
        public string TransactionId { get; set; }
        public string FileName { get; set; }
        public FileDetailType FileDetailType { get; set; }
        public string Description { get; set; }
        public int PageCount { get; set; }
    }
}
