using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace VPay.Distribution.Common
{
    public class StpPaymentFilterDto
    {
        /// <summary>
        /// The transaction ID associated with the payment. Null for any transaction.
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public long? TransactionId { get; set; } = null;

        /// <summary>
        /// The network name. Null for any network.
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public string NetworkName { get; set; } = null;

        /// <summary>
        /// The network enrollment type: B2B or C2B. Null for any enrollment type.
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public string EnrollType { get; set; } = null;

        /// <summary>
        /// The EDI 835 type: PRS or SDS. Null for any EDI 835 type.
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public string Edi835Type { get; set; } = null;

        /// <summary>
        /// The maximum number of results to return. The default is 10,000.
        /// </summary>
        [DefaultValue(10000)]
        public int MaxResults { get; set; } = 10000;
    }
}
