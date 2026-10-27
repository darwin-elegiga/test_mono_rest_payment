using System.Collections.Generic;

namespace VPay.Optum.Nc835Segments
{
    public class Loop1000A
    {
        public N1? PayerIdentification { get; set; }

        public N3? PayerAddress { get; set; }

        public N4? PayerCityStateZip { get; set; }

        public REF? Ref2U { get; set; }

        public REF? RefEO { get; set; }

        public REF? RefHI { get; set; }

        public REF? RefNF { get; set; }

        public PER? PerCX { get; set; }

        public List<PER> PerBL { get; set; } = new List<PER>();

        public PER? PerIC { get; set; }
    }
}
