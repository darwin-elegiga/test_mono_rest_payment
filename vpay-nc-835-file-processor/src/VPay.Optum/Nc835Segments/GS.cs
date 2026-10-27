using System;

namespace VPay.Optum.Nc835Segments
{
    public class GS
    {
        public GS(string[] data)
        {
            if (data.Length < 9)
            {
                throw new ArgumentException("Must have at least 9 strings in the array", nameof(data));
            }

            GS00 = data[0];
            GS01 = data[1];
            GS02 = data[2];
            GS03 = data[3];
            GS04 = data[4];
            GS05 = data[5];
            GS06 = data[6];
            GS07 = data[7];
            GS08 = data[8];
        }

        public string GS00 { get; set; }  //Segment Identifier: GS
        public string GS01 { get; set; }  //Functional Identifier Code
        public string GS02 { get; set; }  //Application Sender's Code
        public string GS03 { get; set; }  //Application Receiver's Code
        public string GS04 { get; set; }  //Date
        public string GS05 { get; set; }  //Time
        public string GS06 { get; set; }  //Group Control Number
        public string GS07 { get; set; }  //Responsible Agency Code
        public string GS08 { get; set; }  //Version/Release/Industry Identifier Code
    }
}
