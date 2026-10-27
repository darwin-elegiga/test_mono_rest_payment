using System;

namespace VPay.Optum.Nc835Segments
{
    public class N1
    {
        public N1(string[] data)
        {
            if (data.Length < 3)
            {
                throw new ArgumentException("Must have at least 3 strings in the array", nameof(data));
            }
            N100 = data[0];
            N101 = data[1];
            N102 = data[2];
            N103 = data.Length >= 4 ? data[3] : "";
            N104 = data.Length >= 5 ? data[4] : "";
        }

        public string N100 { get; set; }  //Segment Identifier: N1
        public string N101 { get; set; }  //Entity identifier code
        public string N102 { get; set; }  //Payer name
        public string N103 { get; set; }  //Identification code qualifier - Optional
        public string N104 { get; set; }  //Identification code - Optional
    }
}
