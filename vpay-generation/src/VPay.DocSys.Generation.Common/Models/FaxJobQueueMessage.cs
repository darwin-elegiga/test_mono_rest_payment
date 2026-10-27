using System;
using Newtonsoft.Json;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public class FaxJobQueueMessage
    {
        private FaxJobQueueMessage(object obj)
        {
            ArgumentHelpers.ThrowIf.IsNull(obj, nameof(obj));

            CorrespondenceId = 0;
            _transactionIds = Array.Empty<long>();
            Payload = JsonConvert.SerializeObject(obj);
        }

        public FaxJobQueueMessage(FaxJobRequest message) : this((object)message)
        {
            CorrespondenceId = message.CorrespondenceId;
            _transactionIds = message.TransactionIds;
            Priority = message.Priority;

            PayloadType = "FaxJobInsertRequest";
        }

        public FaxJobQueueMessage(KillFaxNumberRequest message, int queueMaxPriority)
            : this(message)
        {
            Priority = queueMaxPriority;

            PayloadType = "FaxNumberKillRequest";
        }

        public FaxJobQueueMessage(FaxNumberUpdateRequest message, int queueMaxPriority)
            : this(message)
        {
            CorrespondenceId = message.CorrespondenceId;
            Priority = queueMaxPriority;

            PayloadType = "FaxNumberChangeRequest";
        }

        public FaxJobQueueMessage(FaxJobUpdateRequest message, int queueMaxPriority)
            : this(message)
        {
            CorrespondenceId = message.CorrespondenceId;
            Priority = queueMaxPriority;

            PayloadType = "FaxJobStatusUpdateRequest";
        }

        public DateTime Created => DateTime.Now;

        public int Priority { get; }
        public long CorrespondenceId { get; }

        private readonly long[] _transactionIds;
        public string AdditionalInfo => string.Join(",", _transactionIds);

        public string PayloadType { get; }
        public string Payload { get; }
    }
}
