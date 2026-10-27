using System;
using System.IO;
using System.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public class InventoryDriverHeader
    {
        public InventoryDriverHeader(string headerLine)
        {
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(headerLine, nameof(headerLine));

            var split = headerLine.Split('|').Select(s => s.Trim()).ToArray();

            if(split.Length < 6 || split[0] != "HEADER")
                throw new ArgumentException($"Invalid {nameof(InventoryDriverHeader)} header line.", nameof(headerLine));
            
            this.LineType = split[0];
            this.DriverType = split[1];
            this.DriverFileName = split[2];
            this.iSeriesFilePath = split[3];
            this.DateTimeStamp = split[4];
            this.DriverFileID = int.Parse(split[5]);
        }

        public InventoryDriverHeader(InventoryDriverHeader copySource)
        {
            ArgumentHelpers.ThrowIf.IsNull(copySource, nameof(copySource));

            this.LineType = copySource.LineType;
            this.DriverType = copySource.DriverType;
            this.DriverFileName = copySource.DriverFileName;
            this.iSeriesFilePath = copySource.iSeriesFilePath;
            this.DateTimeStamp = copySource.DateTimeStamp;
            this.DriverFileID = copySource.DriverFileID;
        }

        public string LineType { get; set; }
        public string DriverType { get; set; }
        public string DriverFileName { get; set; }
        public string iSeriesFilePath { get; set; }
        public string DateTimeStamp { get; set; }
        public int DriverFileID { get; set; }

        public void Write(TextWriter writer)
        {
            writer.WriteDriverFileValue(this.LineType, 10);
            writer.WriteDriverFileValue(this.DriverType, 10);
            writer.WriteDriverFileValue(this.DriverFileName, 256);
            writer.WriteDriverFileValue(this.iSeriesFilePath, 256);
            writer.WriteDriverFileValue(this.DateTimeStamp, 26);
            writer.WriteDriverFileValue(this.DriverFileID.ZeroPad(9), 20);
            writer.WriteLine();
        }
    }
}
