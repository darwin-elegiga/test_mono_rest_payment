using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public class FaxMessageCorrespondenceMessage : FaxMessageBase<FaxMessageCorrespondencePayload>
    {
        public FaxMessageCorrespondenceMessage(FaxMessageCorrespondencePayload payload)
        : base()
        {
            ArgumentHelpers.ThrowIf.IsNull(payload, nameof(payload));

            Payload = payload;
        }

        public override string FromSystem => "FAXNET";
        public override string ToSystem => "FAXISERIES";
        public override long MessageIndex => 5;
    }
}
