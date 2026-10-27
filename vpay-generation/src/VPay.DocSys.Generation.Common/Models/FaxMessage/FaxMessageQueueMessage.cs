using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public class FaxMessageQueueMessage : FaxMessageBase<FaxMessagePayload>
    {
        public FaxMessageQueueMessage(string message) : base(message) { }

        public FaxMessageQueueMessage(FaxMessagePayloadRequest payload)
        : base()
        {
            ArgumentHelpers.ThrowIf.IsNull(payload, nameof(payload));

            OriginAction = "CALL";
            Priority = payload.Priority;

            Payload = payload;
        }

        public override string FromSystem => "FAXISERIES";
        public override string ToSystem => "FAXNET";
        public override long MessageIndex => 5;
    }
}
