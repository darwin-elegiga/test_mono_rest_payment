namespace VPay.Optum.Nc835Segments
{
    public class PER
    {
        public PER(string perRecord, string segmentChar)
        {
            var perData = perRecord.Split(segmentChar);
            PER00 = perData[0];
            PER01 = perData[1];
            PER02 = perData[2];
            PER03 = perData[3];
            PER04 = perData[4];
            PER05 = perData[5];
            PER06 = perData[5];
            PER07 = perData[6];
            PER08 = perData[7];
        }

        public string PER00 { get; set; }  //Segment Identifier: PER
        public string PER01 { get; set; }  //Contact function code (CX=Payer Contact Information, BL=Payer Technical Contact Information)
        public string PER02 { get; set; }  //Payer contact name
        public string PER03 { get; set; }  //Communication number qualifier
        public string PER04 { get; set; }  //Communication number
        public string PER05 { get; set; }  //Communication number qualifier
        public string PER06 { get; set; }  //Communication number
        public string PER07 { get; set; }  //Communications number qualifier
        public string PER08 { get; set; }  //Communication number
    }
}
