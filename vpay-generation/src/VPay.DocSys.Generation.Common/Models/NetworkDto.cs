using System;

namespace VPay.DocSys.Generation.Common.Models
{
    public class NetworkDto
    {
        /// <summary>
        /// The Network ID.
        /// </summary>
        public long NetworkId { get; set; }

        /// <summary>
        /// The Network Partner Name.
        /// </summary>
        public string NetworkPartner { get; set; } = string.Empty;

        /// <summary>
        /// The Network Type.
        /// </summary>
        public string NetworkType { get; set; } = string.Empty;

        /// <summary>
        /// The Priority Level.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// The default Payment Type.
        /// </summary>
        public string PaymentType { get; set; } = string.Empty;

        /// <summary>
        /// The default Distribution Method.
        /// </summary>
        public string DistributionMethod { get; set; } = string.Empty;

        /// <summary>
        /// The Network record Active or Inactive Status.
        /// </summary>
        public string ActiveStatus { get; set; } = string.Empty;

        /// <summary>
        /// The user or program that created the record.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// The date when the record was created.
        /// </summary>
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// The user or program that modified the record.
        /// </summary>
        public string LastModifiedBy { get; set; } = string.Empty;

        /// <summary>
        /// The date when the record was last modified.
        /// </summary>
        public DateTime LastModifiedDateTime { get; set; }
    }
}
