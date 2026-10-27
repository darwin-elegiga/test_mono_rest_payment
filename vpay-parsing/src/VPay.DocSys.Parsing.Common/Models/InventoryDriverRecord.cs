using System;
using System.IO;
using System.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public class InventoryDriverRecord
    {
        public InventoryDriverRecord(string recordLine)
        {
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(recordLine, nameof(recordLine));

            var split = recordLine.Split('|').Select(s => s.Trim()).ToArray();

            if (split.Length < 17 || split[0] != "DETAIL")
                throw new ArgumentException($"Invalid {nameof(InventoryDriverRecord)} detail line.", nameof(recordLine));

            this.LineType = split[0];
            this.CPaymentID = split[1];
            this.DocumentType = (RedCardDocumentType)Enum.Parse(typeof(RedCardDocumentType), split[2]);
            this.DocFileName = split[3];
            this.R3ID = long.Parse(split[4]);
            this.Guid = Guid.Parse(split[5]);
            this.Origin = split[6];
            this.TPA = split[7];
            this.BillingEntity = split[8];
            this.Group01 = split[9];
            this.Group02 = split[10];
            this.Group03 = split[11];
            this.Group04 = split[12];
            this.Group05 = split[13];
            this.Group06 = split[14];
            this.ArchiveKey = split[15];
            this.TransactionID = long.Parse(split[16]);
        }

        public InventoryDriverRecord(InventoryDriverRecord copySource)
        {
            ArgumentHelpers.ThrowIf.IsNull(copySource, nameof(copySource));

            this.LineType = copySource.LineType;
            this.CPaymentID = copySource.CPaymentID;
            this.DocumentType = copySource.DocumentType;
            this.DocFileName = copySource.DocFileName;
            this.R3ID = copySource.R3ID;
            this.Guid = copySource.Guid;
            this.Origin = copySource.Origin;
            this.TPA = copySource.TPA;
            this.BillingEntity = copySource.BillingEntity;
            this.Group01 = copySource.Group01;
            this.Group02 = copySource.Group02;
            this.Group03 = copySource.Group03;
            this.Group04 = copySource.Group04;
            this.Group05 = copySource.Group05;
            this.Group06 = copySource.Group06;
            this.ArchiveKey = copySource.ArchiveKey;
            this.TransactionID = copySource.TransactionID;
        }

        public string LineType { get; set; }
        public string CPaymentID { get; set; }
        public RedCardDocumentType DocumentType { get; set; }
        public string DocFileName { get; set; }
        public long R3ID { get; set; }
        public Guid Guid { get; set; }
        public string Origin { get; set; }
        public string TPA { get; set; }
        public string BillingEntity { get; set; }
        public string Group01 { get; set; }
        public string Group02 { get; set; }
        public string Group03 { get; set; }
        public string Group04 { get; set; }
        public string Group05 { get; set; }
        public string Group06 { get; set; }
        public string ArchiveKey { get; set; }
        public long TransactionID { get; set; }

        public void Write(TextWriter writer)
        {
            writer.WriteDriverFileValue(this.LineType, 10);
            writer.WriteDriverFileValue(this.CPaymentID, 41);
            writer.WriteDriverFileValue(this.DocumentType.ToString(), 2);
            writer.WriteDriverFileValue(this.DocFileName, 256);
            writer.WriteDriverFileValue(this.R3ID.ZeroPad(20), 20);
            writer.WriteDriverFileValue(this.Guid.ToString(), 50);
            writer.WriteDriverFileValue(this.Origin, 20);
            writer.WriteDriverFileValue(this.TPA, 10);
            writer.WriteDriverFileValue(this.BillingEntity, 20);
            writer.WriteDriverFileValue(this.Group01, 15);
            writer.WriteDriverFileValue(this.Group02, 15);
            writer.WriteDriverFileValue(this.Group03, 15);
            writer.WriteDriverFileValue(this.Group04, 15);
            writer.WriteDriverFileValue(this.Group05, 15);
            writer.WriteDriverFileValue(this.Group06, 15);
            writer.WriteDriverFileValue(this.ArchiveKey, 10);
            writer.WriteDriverFileValue(this.TransactionID.ZeroPad(20), 25);
            writer.WriteLine();
        }
    }
}

