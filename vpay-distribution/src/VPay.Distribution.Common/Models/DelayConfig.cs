namespace VPay.Distribution.Common.Models
{
    public class DelayConfig
    {
        /// <summary>
        /// Status Check Delay in seconds
        /// </summary>
        public int StatusCheckDelay { get; set; }

        /// <summary>
        /// Warning Issue Delay in seconds
        /// </summary>
        public int WarningIssueDelay { get; set; }
    }
}
