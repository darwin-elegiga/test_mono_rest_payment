using System.Collections.Generic;
using System.ComponentModel;

namespace VPay.Distribution.Common
{
    public class StpPaymentGroupDto
    {
        /// <summary>
        /// The name of the network partner.
        /// </summary>
        [DefaultValue("")]
        public string NetworkName { get; set; } = "";

        /// <summary>
        /// The network enrollment type: B2B or C2B.
        /// </summary>
        [DefaultValue("B2B")]
        public string EnrollType { get; set; } = "B2B";

        /// <summary>
        /// The EDI 835 type: PRS or SDS.
        /// </summary>
        [DefaultValue("SDS")]
        public string Edi835Type { get; set; } = "SDS";

        /// <summary>
        /// The flag indicating whether the remittance is valid.
        /// </summary>
        [DefaultValue(false)]
        public bool RemitValid { get; set; } = false;

        /// <summary>
        /// The flag indicating whether an EDI 835 file is available.
        /// </summary>
        [DefaultValue(false)]
        public bool Edi835Avail { get; set; } = false;

        /// <summary>
        /// The flag indicating whether the payment has been sent.
        /// </summary>
        [DefaultValue(false)]
        public bool PaymentSent { get; set; } = false;

        /// <summary>
        /// The list of transaction IDs associated with the payment group.
        /// </summary>
        public IEnumerable<long> TransactionIds { get; set; } = new List<long>();
    }
}
