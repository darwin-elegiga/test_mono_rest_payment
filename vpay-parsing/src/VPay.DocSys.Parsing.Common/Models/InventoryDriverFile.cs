using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public class InventoryDriverFile
    {
        public InventoryDriverFile(string[] lines)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(lines, nameof(lines));

            if(lines.Length < 2)
            {
                throw new ArgumentException($"Invalid {nameof(InventoryDriverFile)} content.", nameof(lines));
            }

            this.Header = new InventoryDriverHeader(lines[0]);
            this.Records = lines.Skip(1).Take(lines.Length - 2).Select(l => new InventoryDriverRecord(l)).ToList();
            this.Footer = new InventoryDriverFooter(lines[lines.Length - 1]);

            if (Footer.RecordCount != Records.Count)
            {
                throw new ArgumentException($"Footer count ({Footer.RecordCount}) does not match Record count ({Records.Count})");
            }
        }

        public InventoryDriverFile(InventoryDriverFile copySource)
        {
            ArgumentHelpers.ThrowIf.IsNull(copySource, nameof(copySource));

            this.Header = new InventoryDriverHeader(copySource.Header);
            this.Records = copySource.Records.Select(r => new InventoryDriverRecord(r)).ToList();
            this.Footer = new InventoryDriverFooter(copySource.Footer);
        }

        public InventoryDriverFile(InventoryDriverHeader header, List<InventoryDriverRecord> records, InventoryDriverFooter footer)
        {
            ArgumentHelpers.ThrowIf.IsNull(header, nameof(header));
            ArgumentHelpers.ThrowIf.IsNull(records, nameof(records));
            ArgumentHelpers.ThrowIf.IsNull(footer, nameof(footer));

            this.Header = header;
            this.Records = records;
            this.Footer = footer;
        }

        public InventoryDriverHeader Header { get; set; }
        public List<InventoryDriverRecord> Records { get; set; }
        public InventoryDriverFooter Footer { get; set; }

        public void Write(string filePath)
        {
            var fileSystem = new FileSystem();

            using(var stream = fileSystem.File.OpenWrite(filePath))
            using(var writer = new StreamWriter(stream))
            {
                this.Write(writer);
            }
        }

        public void Write(TextWriter writer)
        {
            this.Footer.RecordCount = this.Records.Count;

            this.Header.Write(writer);

            foreach(var record in this.Records)
            {
                record.Write(writer);
            }

            this.Footer.Write(writer);
        }
    }
}
