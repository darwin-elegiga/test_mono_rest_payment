using System.Collections.Generic;

namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public class FinSegFile
    {
        public string FileName { get; }
        public FinSegFileHeader Header { get; set; }
        public List<FinSegFileDetail> Details { get; }
        public FinSegFileTrailer Trailer { get; set; }

        public FinSegFile(string fileName, List<FinSegFileDetail> details)
        {
            FileName = fileName;

            Header = new FinSegFileHeader { FileName = fileName };
            Details = details;
            Trailer = new FinSegFileTrailer { DetailCount = details.Count };
        }
    }
}
