using System;

namespace VPay.Optum.Nc835Segments
{
    public class ST
    {
        public ST(string[] data)
        {
            if (data.Length < 3)
            {
                throw new ArgumentException("Must have at least 3 strings in the array", nameof(data));
            }

            ST00 = data[0];
            ST01 = data[1];
            ST02 = data[2];
        }

        public string ST00 { get; set; }  //Segment Identifier: ST
        public string ST01 { get; set; }  //Transaction set identifier code
        public string ST02 { get; set; }  //Transaction set control number
    }
}
