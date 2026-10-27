using System.Diagnostics.CodeAnalysis;

namespace VPay.Optum.PaymentRouting.Data
{
    [ExcludeFromCodeCoverage]

    public static class SqlErrorCode
    {
        public const int FOREIGN_KEY_REFERENCE_CONSTRAINT = 547;
        public const int UNIQUE_INDEX_VIOLATION = 2601;
        public const int DUPLICATE_KEY_VIOLATION = 2627;
    }
}
