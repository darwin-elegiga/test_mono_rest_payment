using System;

namespace VPay.Optum.Nc835Segments
{
    public class BPR
    {
        public BPR(string[] data)
        {
            if (data.Length < 17)
            {
                throw new ArgumentException("Must have at least 17 strings in the array", nameof(data));
            }

            BPR00 = data[0];
            BPR01 = data[1];
            BPR02 = data[2];
            BPR03 = data[3];
            BPR04 = data[4];
            BPR05 = data[5];
            BPR06 = data[6];
            BPR07 = data[7];
            BPR08 = data[8];
            BPR09 = data[9];
            BPR10 = data[10];
            BPR11 = data[11];
            BPR12 = data[12];
            BPR13 = data[13];
            BPR14 = data[14];
            BPR15 = data[15];
            BPR16 = data[16];
        }

        public string BPR00 { get; set; }  //Segment Identifier: BPR
        public string BPR01 { get; set; }  //Transaction Handling code
        public string BPR02 { get; set; }  //Total actual provider payment amoun
        public string BPR03 { get; set; }  //Credit/Debit flag
        public string BPR04 { get; set; }  //Payment method code
        public string BPR05 { get; set; }  //Payment format code
        public string BPR06 { get; set; }  //DFI ID number qualifier
        public string BPR07 { get; set; }  //Payer DFI Identification number
        public string BPR08 { get; set; }  //Payer account number qualifier
        public string BPR09 { get; set; }  //Sender (payer) bank account number
        public string BPR10 { get; set; }  //Payer identifier
        public string BPR11 { get; set; }  //Originating company supplemental identifier
        public string BPR12 { get; set; }  //DFI Identification number qualifier (receiver)
        public string BPR13 { get; set; }  //Receiver or provider bank ID number
        public string BPR14 { get; set; }  //Receiver Account number qualifier
        public string BPR15 { get; set; }  //Receiver or provider account number
        public string BPR16 { get; set; }  //Check issue or EFT effective date
    }
}
