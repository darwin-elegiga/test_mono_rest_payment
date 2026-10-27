using System;
using System.IO;
using System.Text;
using System.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public class InventoryDriverFooter
    {
        public InventoryDriverFooter(string footerLine)
        {
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(footerLine, nameof(footerLine));

            var split = footerLine.Split('|').Select(s => s.Trim()).ToArray();

            if(split.Length < 4 || split[0] != "FOOTER")
                throw new ArgumentException($"Invalid {nameof(InventoryDriverFooter)} footer line.", nameof(footerLine));

            this.LineType = split[0];
            this.RecordCount = long.Parse(split[1]);
            this.TotalAmount = long.Parse(split[2]);
        }

        public InventoryDriverFooter(InventoryDriverFooter copySource)
        {
            ArgumentHelpers.ThrowIf.IsNull(copySource, nameof(copySource));

            this.LineType = copySource.LineType;
            this.RecordCount = copySource.RecordCount;
            this.TotalAmount = copySource.TotalAmount;
        }

        public string LineType { get; set; }
        public long RecordCount { get; set; }
        public long TotalAmount { get; set; }

        public void Write(TextWriter writer)
        {
            writer.WriteDriverFileValue(this.LineType, 10);
            writer.WriteDriverFileValue(this.RecordCount.ZeroPad(20), 20);
            writer.WriteDriverFileValue(this.TotalAmount.ZeroPad(20), 20);
            writer.Write(string.Empty.PadRight(10));
            writer.WriteLine();
        }
    }
}
