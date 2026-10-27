namespace VPay.Distribution.Common
{
    public class DistributionProcessResult
    {
        public DistributionProcessResult(string distributionChannel)
        {
            this.DistributionChannel = distributionChannel;
            this.FileName = string.Empty;
            this.ControlFileName = string.Empty;
        }

        public string DistributionChannel { get; set; }

        public int Count { get; set; }

        public string FileName { get; set; }

        public string ControlFileName { get; set; }
    }
}