namespace VPay.Optum.Nc835Segments
{
    public class REF
    {
        public REF(string refRecord, string segmentChar)
        {
            var refData = refRecord.Split(segmentChar);
            REF00 = refData[0];
            REF01 = refData[1];
            REF02 = refData[2];
        }

        public string REF00 { get; set; }  //Segment Identifier: REF
        public string REF01 { get; set; }  //Reference identification qualifier - EV=receiver of the transaction is other than the payee or 2U=Required Payables
        public string REF02 { get; set; }  //Receiver Identifier
    }
}
