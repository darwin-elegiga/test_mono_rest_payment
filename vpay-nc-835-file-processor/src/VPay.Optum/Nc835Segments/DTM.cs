using System;

namespace VPay.Optum.Nc835Segments
{
    public class DTM
    {
        public DTM(string[] data)
        {
            if (data.Length < 3)
            {
                throw new ArgumentException("Must have at least 3 strings in the array", nameof(data));
            }

            DTM00 = data[0];
            DTM01 = data[1];
            DTM02 = data[2];
        }

        public string DTM00 { get; set; }

        public string DTM01 { get; set; }

        public string DTM02 { get; set; }
    }
}
