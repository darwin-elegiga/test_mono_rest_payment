using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace VPay.DocSys.Generation.Models
{
    /// <summary>
    /// Queue message for Bundled Fax
    /// </summary>
    public class FaxMessage
    {
        public FaxMessage()
        {
            MessageId = Guid.NewGuid();
        }

        public string MessageType =>"PMB"; 
        
        public string Version =>"00.00.0001"; 
        
        public string FromSystem=> "FAXNET".PadRight(20);

        public string ToSystem => "FAXISERIES".PadRight(20);

        public string Family => "FAXDIST".PadRight(20);

        public string Priority => "0000000001";

        public string FormatId {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(SmallFamily);
                sb.Append(TypeOfMessage);
                sb.Append(Severity);
                sb.Append(MessageIndex);
                //Another version field
                sb.Append("0001");
                sb.Append("    ");
                return sb.ToString();

            }
        }

        public Guid MessageId { get; set; }

        public string OriginAction =>"REQUEST".PadRight(20);

        public string CreateTimeStamp => DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss:ffffff");

        public string HopCount=>"0";

        public string OrgFormatId => new string(' ',20);

        public string OrgMessageId => "n/a".PadRight(32);

        public string Padding => new string(' ', 41);
        
        public string SmallFamily =>"FAX"; 
        
        public string TypeOfMessage =>"I"; 
        
        public string Severity =>"99"; 

        /// <summary>
        /// This is what tells the iSeries what kind of message it is
        /// </summary>
        public string MessageIndex => "000004";

        public string FormatVersion =>"0001"; 
        
        /// <summary>
        /// Number of pages in fax
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// Path to PDf on filesystem
        /// </summary>
        public string PDFLocation { get; set; }

        /// <summary>
        /// Comma separated list of transaction Ids
        /// </summary>
        public List<string> TransactionIds { get; set; }
        /// <summary>
        /// Fax Destination Phone number 
        /// </summary>
        public string FaxNumber { get; set; }

        public string faxMessageData
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(PageCount.ToString("D5"));
                sb.Append(" ");
                sb.Append(PDFLocation.PadRight(226));
                sb.Append(" ");
                FaxNumber = Regex.Replace(FaxNumber, @"\D", string.Empty);
                if (FaxNumber.Length == 10)
                {
                    FaxNumber = "1" + FaxNumber;
                }

                FaxNumber = Regex.Replace(FaxNumber, @"(\d)(\d{3})(\d{3})(\d{4})", "$1-$2-$3-$4");
                sb.Append(FaxNumber.PadRight(14));
                sb.Append(" ");
                sb.Append(string.Join(",", TransactionIds));
                //Message terminator so ISeries doesn't read past end of message
                sb.Append(",|");
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MessageType);
            sb.Append(Version);
            sb.Append(FromSystem);
            sb.Append(ToSystem);
            sb.Append(Family);
            sb.Append(Priority);
            sb.Append(FormatId);
            sb.Append(MessageId.ToString().Replace("-",string.Empty));
            sb.Append(OriginAction);
            sb.Append(CreateTimeStamp);
            sb.Append(HopCount);
            sb.Append(OrgFormatId);
            sb.Append(OrgMessageId);
            sb.Append(Padding);
            sb.Append(faxMessageData);
            return sb.ToString();
        }
    }
}
