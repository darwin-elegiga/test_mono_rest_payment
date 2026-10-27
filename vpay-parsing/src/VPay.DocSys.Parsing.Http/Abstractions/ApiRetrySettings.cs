namespace VPay.DocSys.Parsing.Http.Abstractions
{
    public class ApiRetrySettings
    {
        /// <summary>
        /// Gets or sets the retry count for the retry policy.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the wait time (in milliseconds) for the retry policy
        /// </summary>
        public int MinWaitMs { get; set; }

        /// <summary>
        /// Gets or sets the wait time (in milliseconds) for the retry policy
        /// </summary>
        public int MaxWaitMs { get; set; }
    }
}
