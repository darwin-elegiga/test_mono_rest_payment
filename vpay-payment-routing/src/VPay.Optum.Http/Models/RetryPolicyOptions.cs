namespace VPay.Optum.Http.Models
{
    public class RetryPolicyOptions
    {
        /// <summary>
        /// Gets or sets the retry count for the retry policy.
        /// </summary>
        public int Count { get; set; } = 3;

        /// <summary>
        /// Gets or sets the wait time (in milliseconds) for the retry policy
        /// </summary>
        public int Wait { get; set; } = 250;
    }
}
