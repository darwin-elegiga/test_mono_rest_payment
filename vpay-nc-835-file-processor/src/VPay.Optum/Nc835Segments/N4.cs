using System;

namespace VPay.Optum.Nc835Segments
{
    public class N4
    {
        public N4(string[] data)
        {
            if (data.Length < 1)
            {
                throw new ArgumentException("Must have at least 1 strings in the array", nameof(data));
            }
            N400 = data[0];
            N401 = data.Length > 1 ? data[1] : "";
            N402 = data.Length > 2 ? data[2] : "";
            N403 = data.Length > 3 ? data[3] : "";

            N404 = data.Length > 4 ? data[4] : "";
            N405 = data.Length > 5 ? data[5] : "";
            N406 = data.Length > 6 ? data[6] : "";
            N407 = data.Length > 7 ? data[7] : "";
        }

        public string N400 { get; set; }  //Segment Identifier: N4
        public string N401 { get; set; }  //City name
        public string N402 { get; set; }  //State code
        public string N403 { get; set; }  //Postal (zip) code
        public string N404 { get; set; }  //Country Code
        public string N405 { get; set; }  //Location Qualifier
        public string N406 { get; set; }  //Location Identifier
        public string N407 { get; set; }  //Country Subdivision Code
    }
}
