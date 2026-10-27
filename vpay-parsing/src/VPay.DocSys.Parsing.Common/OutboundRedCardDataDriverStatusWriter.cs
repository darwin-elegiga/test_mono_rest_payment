using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing.Common
{
    public class OutboundRedCardDataDriverStatusWriter : FileWriterBase, IOutboundRedCardDataDriverStatusWriter
    {
        public OutboundRedCardDataDriverStatusWriter(IFileSystem fileSystem) : base(fileSystem) { }

        public void Write(TextWriter textWriter, OutboundRedCardDataDriverFile outboundRedCardDataDriverFile)
        {
            textWriter.WriteLine(CreateHeader());

            foreach (var r in outboundRedCardDataDriverFile.Records)
            {
                textWriter.WriteLine(CreateRecord(r));
            }

            textWriter.WriteLine(CreateFooter(outboundRedCardDataDriverFile.Records.Select(_ => _.Status).ToList()));
        }

        private string CreateHeader()
        {
            return $"00|{DateTimeStamp}|status file";
        }

        private string CreateRecord(OutboundRedCardDataDriverRecord r)
        {
            return $"04|{r.Guid}|{r.CoverFileName}|{r.MemberFileName}|{r.Status.GetDescription()}";
        }

        private string CreateFooter(IList<OutboundRedCardDataDriverRecordStatus> statusList)
        {
            var validated = statusList.Count(_ => _ == OutboundRedCardDataDriverRecordStatus.Validated);
            var failures = statusList.Count - validated;

            return $"99|{DateTimeStamp}|{validated}|{failures}";
        }
    }
}
