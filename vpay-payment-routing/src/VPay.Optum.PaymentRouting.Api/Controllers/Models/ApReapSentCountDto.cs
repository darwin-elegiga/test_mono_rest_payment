using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models
{
    public class ApReapSentCountDto
    {
        /// <summary>
        /// The number of AP/REAP messages requested to be sent out.
        /// </summary>
        public int Requested { get; set; }

        /// <summary>
        /// The number of AP/REAP messages found in the <see cref="ApMessage"/> database table.
        /// </summary>
        public int Found { get; set; }

        /// <summary>
        /// The number of AP/REAP messages sent out.
        /// </summary>
        public int Sent { get; set; }

        /// <summary>
        /// The number of found messages that are not Azure bound (OPACH, CLCFT or Enrolled CLNPF).
        /// </summary>
        public int NotSent { get; set; }
    }
}
