using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Models
{
    /// <summary>
    /// Holds OTB data.
    /// </summary>
    public class OtbBillingEntityClientData
    {
        /// <summary>
        /// The origin code.
        /// </summary>
        public string OriginCode { get; }

        /// <summary>
        /// The origin name.
        /// </summary>
        public string OriginName { get; }

        /// <summary>
        /// The client code.
        /// </summary>
        public string ClientCode { get; }

        /// <summary>
        /// The dictionary of billing entities having the billing entity code as the key and having
        /// each dictionary of billing entity fields as the value where each dictionary of billing
        /// entity fields contains the field name as the key and field value as the dictionary value.
        /// </summary>
        public IDictionary<string, IDictionary<string, string>> BillingEntities { get; }

        /// <summary>
        /// Produces a fully initialized implementation of <see cref="OtbBillingEntityClientData"/>
        /// </summary>
        /// <param name="originCode">The origin code. Must not be null.</param>
        /// <param name="originName">The origin name. Must not be null.</param>
        /// <param name="clientCode">The client code. Must not be null.</param>
        /// <param name="billingEntities">The dictionary of billing entities having the billing entity
        /// code as the key and having each dictionary of billing entity fields as the value where each
        /// dictionary of billing entity fields contains the field name as the key and field value as
        /// the dictionary value. Must not be null.</param>
        public OtbBillingEntityClientData(
            string originCode,
            string originName,
            string clientCode,
            IDictionary<string, IDictionary<string, string>> billingEntities)
        {
            ArgumentHelpers.ThrowIf.IsNull(originCode, nameof(originCode));
            ArgumentHelpers.ThrowIf.IsNull(originName, nameof(originName));
            ArgumentHelpers.ThrowIf.IsNull(clientCode, nameof(clientCode));
            ArgumentHelpers.ThrowIf.IsNull(billingEntities, nameof(billingEntities));

            OriginCode = originCode;
            OriginName = originName;
            ClientCode = clientCode;
            BillingEntities = billingEntities;
        }

        /// <summary>
        /// Produces an empty implementation of <see cref="OtbBillingEntityClientData"/>
        /// </summary>
        /// <returns></returns>
        public static OtbBillingEntityClientData Empty()
        {
            return new OtbBillingEntityClientData(
                originCode: string.Empty,
                originName: string.Empty,
                clientCode: string.Empty,
                billingEntities: new Dictionary<string, IDictionary<string, string>>());
        }
    }
}
