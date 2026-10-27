using System;

namespace VPay.Optum.Nc835Segments
{
    public class IEA
    {
        public IEA(string[] data)
        {
            if (data.Length < 3)
            {
                throw new ArgumentException("Must have at least 3 strings in the array", nameof(data));
            }
            IEA00 = data[0];
            IEA01 = data[1];
            IEA02 = data[2];
        }

        public string IEA00 { get; set; }  //Segment Identifier: IEA
        public string IEA01 { get; set; }  //Number of included Functional Groups
        public string IEA02 { get; set; }  //Interchange Control Number
    }
}
