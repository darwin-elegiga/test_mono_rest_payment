using System;

namespace VPay.Optum.Nc835Segments
{
    public class SE
    {
        public SE(string[] data)
        {
            if (data.Length < 3)
            {
                throw new ArgumentException("Must have at least 3 strings in the array", nameof(data));
            }

            SE00 = data[0];
            SE01 = data[1];
            SE02 = data[2];
        }

        public string SE00 { get; set; }  //Segment Identifier: SE
        public string SE01 { get; set; }  //Number of Included Segments
        public string SE02 { get; set; }  //Transaction set control number
    }
}
