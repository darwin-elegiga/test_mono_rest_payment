namespace VPay.Optum.Http.Loaf.Constants
{
    public static class AuthorizationHeader
    {
        private const string Authorization = "Authorization: ";

        public const string Basic = Authorization + "Basic";

        public const string Bearer = Authorization + "Bearer";

        public const string Digest = Authorization + "Digest";

        public const string Hoba = Authorization + "HOBA";

        public const string Mutual = Authorization + "Mutual";

        public const string Aws4HmacSha256 = Authorization + "AWS4-HMAC-SHA256";
    }
}
