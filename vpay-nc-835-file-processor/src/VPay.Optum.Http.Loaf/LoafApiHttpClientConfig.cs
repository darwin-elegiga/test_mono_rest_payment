namespace VPay.Optum.Http.Loaf
{
    /// <summary>
    /// Configuration related specifically to the LOAF API.
    /// </summary>
    public class LoafApiHttpClientConfig : HttpClientConfig
    {
        public string UserName { get; set; }

        public string Password { get; set; }
    }
}
