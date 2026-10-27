using System;

namespace VPay.Optum.Nc835Segments
{
    public class N3
    {
        public N3(string[] data)
        {
            if (data.Length < 2)
            {
                throw new ArgumentException("Must have at least 2 strings in the array", nameof(data));
            }
            N300 = data[0];
            N301 = data[1];
        }

        public string N300 { get; set; }  //Segment Identifier: N3
        public string N301 { get; set; }  //Payer address line
    }
}
