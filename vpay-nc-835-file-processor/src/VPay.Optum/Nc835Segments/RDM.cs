using System;

namespace VPay.Optum.Nc835Segments
{
    public class RDM
    {
        public RDM(string record, string segmentChar)
        {
            var arr = record.Split(segmentChar);

            if (arr.Length < 3)
            {
                throw new ArgumentException("Must have at least 3 strings in the array", nameof(arr));
            }

            RDM00 = arr[0];
            RDM01 = arr[1];
            RDM02 = arr[2];
        }

        public string RDM00 { get; set; }

        public string RDM01 { get; set; }

        public string RDM02 { get; set; }
    }
}
