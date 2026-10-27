namespace VPay.Optum.Http.ReconReportsApi
{
    /// <summary>
    /// Configuration related specifically to the Recon Reports API.
    /// </summary>
    public class ReconReportsApiHttpClientConfig : HttpClientConfig
    {
        /// <summary>
        /// Gets or sets the access token for the Recon Reports API.
        /// </summary>
        public string ApiToken { get; set; } = string.Empty;
    }
}
