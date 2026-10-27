using System;

namespace VPay.Optum.Nc835Segments
{
    public class GE
    {
        public GE(string[] data)
        {
            if (data.Length < 3)
            {
                throw new ArgumentException("Must have at least 3 strings in the array", nameof(data));
            }

            GE00 = data[0];
            GE01 = data[1];
            GE02 = data[2];
        }

        public string GE00 { get; set; }  //Segment Identifier: GE
        public string GE01 { get; set; }  //Number of Transaction Sets Included
        public string GE02 { get; set; }  //Transaction set control number
    }
}
