using System;

namespace VPay.Optum.Nc835Segments
{
    public class ISA
    {
        public ISA(string isaRecord)
        {
            if (!isaRecord.StartsWith("ISA"))
            {
                throw new ArgumentException("ISA record must start with ISA", nameof(isaRecord));
            }
            else if (isaRecord.Length != 106)
            {
                throw new ArgumentException("ISA record must be 106 characters long", nameof(isaRecord));
            }
            SegmentSeparator = isaRecord[3];

            var isaData = isaRecord.Split(SegmentSeparator);

            if (isaData.Length != 17)
            {
                throw new ArgumentException("ISA must have at 17 parts", nameof(isaRecord));
            }

            ISA00 = isaData[0];
            ISA01 = isaData[1];
            ISA02 = isaData[2];
            ISA03 = isaData[3];
            ISA04 = isaData[4];
            ISA05 = isaData[5];
            ISA06 = isaData[6];
            ISA07 = isaData[7];
            ISA08 = isaData[8];
            ISA09 = isaData[9];
            ISA10 = isaData[10];
            ISA11 = isaData[11];
            ISA12 = isaData[12];
            ISA13 = isaData[13];
            ISA14 = isaData[14];
            ISA15 = isaData[15];
            ComponentSeparator = isaData[16][0];
            EndSegmentChar = isaData[16][1];
        }

        public string ISA00 { get; set; }  //Segment Identifier: ISA
        public string ISA01 { get; set; }  //Authorization Information Qualifier
        public string ISA02 { get; set; }  //Authorization Information
        public string ISA03 { get; set; }  //Security Information Qualifier
        public string ISA04 { get; set; }  //Security Information
        public string ISA05 { get; set; }  //Interchange ID Qualifier
        public string ISA06 { get; set; }  //Interchange Sender ID
        public string ISA07 { get; set; }  //Interchange ID Qualifier
        public string ISA08 { get; set; }  //Interchange Receiver ID
        public string ISA09 { get; set; }  //Interchange Date
        public string ISA10 { get; set; }  //Interchange Time
        public string ISA11 { get; set; }  //Repetition Separator
        public string ISA12 { get; set; }  //Interchange Control Version Number
        public string ISA13 { get; set; }  //Interchange Control Number
        public string ISA14 { get; set; }  //Acknowledgement Requested
        public string ISA15 { get; set; }  //Usage Indicator

        public char ComponentSeparator { get; set; }  //Component Element Separator
        public char EndSegmentChar { get; set; }  // End of Segment Character

        public char SegmentSeparator { get; set; }
    }
}
