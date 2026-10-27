using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common.Helpers
{
    public static class DocGenParamHelper
    {
        public static string GetValue(this DocGenParam docGenParam, string redCardOtbRecordKey, string nonRedCardValue)
        {
            ArgumentHelpers.ThrowIf.IsNull(docGenParam, nameof(docGenParam));
            ArgumentHelpers.ThrowIf.IsNull(redCardOtbRecordKey, nameof(redCardOtbRecordKey));

            return docGenParam.IsRedCard
                ? docGenParam.OtbRecord.TryGetValueDefault(redCardOtbRecordKey)
                : nonRedCardValue;
        }
    }
}
