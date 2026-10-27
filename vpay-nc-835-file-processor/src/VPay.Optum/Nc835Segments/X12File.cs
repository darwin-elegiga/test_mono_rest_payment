using System.Collections.Generic;

namespace VPay.Optum.Nc835Segments
{
    public class X12File
    {
        public ISA? ISA { get; set; }

        public GS? GS { get; set; }

        public List<X12Record> Records { get; } = new List<X12Record>();

        public GE? GE { get; set; }

        public IEA? IEA { get; set; }
    }
}
