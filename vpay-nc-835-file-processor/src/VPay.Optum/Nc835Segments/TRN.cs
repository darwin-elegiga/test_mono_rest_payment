using System;

namespace VPay.Optum.Nc835Segments
{
    public class TRN
    {
        public TRN(string[] data)
        {
            if (data.Length < 5)
            {
                throw new ArgumentException("Must have at least 5 strings in the array", nameof(data));
            }
            TRN00 = data[0];
            TRN01 = data[1];
            TRN02 = data[2];
            TRN03 = data[3];
            TRN04 = data[4];
        }

        public string TRN00 { get; set; }  //Segment Identifier: TRN
        public string TRN01 { get; set; }  //Trace type code
        public string TRN02 { get; set; }  //Check or EFT Trace number
        public string TRN03 { get; set; }  //Originating company identifier
        public string TRN04 { get; set; }  //Originating company supplemental identifier
    }
}
