using VPay.DocSys.Data;

namespace VPay.Distribution.Common.Models
{
    public class ClearDistributionStatusResults
    {
        public bool IsMailCleared { get; set; }
        public bool IsFaxCleared { get; set; }
        public bool IsSuccess(DistributionMethod distMethod)
        {
            if (distMethod == DistributionMethod.Fax)
            {
                return IsFaxCleared;
            }
            else if (distMethod == DistributionMethod.Mail)
            {
                return IsMailCleared;
            }
            else
            {
                return IsFaxCleared && IsMailCleared;
            }
        }
    }
}
