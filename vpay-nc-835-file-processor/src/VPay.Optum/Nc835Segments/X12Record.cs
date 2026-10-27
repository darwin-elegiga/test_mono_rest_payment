namespace VPay.Optum.Nc835Segments
{
    public class X12Record
    {
        public ST? ST { get; set; }

        public BPR? BPR { get; set; }

        public TRN? TRN { get; set; }

        public DTM? DTM { get; set; }

        public Loop1000A Loop1000A { get; set; } = new Loop1000A();

        public Loop1000B Loop1000B { get; set; } = new Loop1000B();

        public SE? SE { get; set; }
    }
}
