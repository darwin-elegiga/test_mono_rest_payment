using System.Collections.Generic;

namespace VPay.Optum.Nc835Segments
{
    public class Loop1000B
    {
        public N1? PayeeIdentification { get; set; }

        public N3? PayeeAddress { get; set; }

        public N4? PayeeCityStateZip { get; set; }

        public List<REF> Refs { get; set; } = new List<REF>();

        public RDM? RemittanceDeliveryMethod { get; set; }

        public List<PER> PerBL { get; set; } = new List<PER>();

        public PER? PerIC { get; set; }
    }
}
