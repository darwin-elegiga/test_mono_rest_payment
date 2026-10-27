using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public class Edi835BulkUpdateRequest
    {
        public List<long> TransactionIds { get; set; }
        public string Comment { get; set; }
        public string UserName { get; set; }
        public Edi835StatusCode Edi835StatusCode { get; set; }
    }
    public class Edi835BulkUpdateRequest_WillNotReceive835 
    {
        public List<long> TransactionIds { get; set; }
        public string Comment { get; set; }
        public string UserName { get; set; }
 
    }

}
