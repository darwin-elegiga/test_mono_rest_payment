using System.Collections.Generic;
using VPay.DocSys.Data;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Common
{
    public class Edi835StatusHistory
    {
        public Edi835StatusHistory()
        {
        }

        public Edi835StatusHistory(DS.Edi835 edi835, List<Edi835Status> statusHistory)
        {
            Edi835 = edi835;
            StatusHistory = statusHistory;
        }

        public DS.Edi835 Edi835 { get; set; }
        public List<Edi835Status> StatusHistory { get; set; }

        
    }
}
