using VPay.Optum.Http.Models;

namespace VPay.Optum.Http
{
    public class PolicyOptions
    {
        /// <summary>
        /// Gets or sets the options defining the retry policy.
        /// </summary>
        public RetryPolicyOptions HttpRetry { get; set; }
    }
}
