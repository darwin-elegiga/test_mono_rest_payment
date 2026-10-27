using System;
using System.Collections.Generic;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Helpers;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Multiple <<see cref="InventoryDriverRecordMessage"/> that have the same guid and are <see cref="RedCardDocumentType.CO"/> are used to create a <see cref="CoRecord"/>
    /// </summary>
    public class CoRecord
    {
        public CoRecord(InventoryDriverRecordMessage message)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));

            DocumentFileNames = message.DocumentFileNames;

            Guid = message.Guid;
            TransactionId = message.TransactionId;
            PaymentId = message.PaymentId;
            DocumentType = message.DocumentType;
        }

        public Guid Guid { get; set; }
        public long TransactionId { get; set; }
        public IList<string> DocumentFileNames { get; set; }
        public string PaymentId { get; set; }
        public RedCardDocumentType DocumentType { get; set; }

        public ArchiveFileRevisionMetadata GetRevisionMetadata(ICollection<string> fileNames)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(fileNames, nameof(fileNames));

            var revision = 1;
            var fileName = $"{PaymentId}_{DocumentType}_stapled.pdf";

            while (fileNames.ContainsString(fileName))
            {
                revision += 1;
                fileName = ToRevisionFileName(revision);
            }

            return new ArchiveFileRevisionMetadata
            {
                FileName = fileName,
                Description = ToRevisionDescription(revision),
                Revision = revision,
            };
        }

        private string ToRevisionFileName(int revision)
        {
            return $"{PaymentId}_{DocumentType}_stapled{revision.PrependToRevisionString("_")}.pdf";
        }

        private static string ToRevisionDescription(int revision)
        {
            return $"stapled mailed pdf documents(s){revision.PrependToRevisionString(" v")}";
        }
    }
}
