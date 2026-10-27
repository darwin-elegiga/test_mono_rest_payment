using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Common.Models;

namespace VPay.DocSys.Parsing
{
    public class OtbDataService : IOtbDataService
    {
        private readonly OtbDataServiceSettings _settings;
        private readonly IPlatformConfigurationApiClient _otbApi;
        private readonly ILogger _logger;

        public OtbDataService(
            OtbDataServiceSettings settings,
            IPlatformConfigurationApiClient otbApi,
            ILogger<OtbDataService> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(settings, nameof(settings));
            ArgumentHelpers.ThrowIf.IsNull(otbApi, nameof(otbApi));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _settings = settings;
            _otbApi = otbApi;
            _logger = logger;
        }

        public bool UseApi => _settings.UseApi;

        public bool UseFile => _settings.UseFile;

        public async Task<OtbBillingEntityClientData> GetOtbDataAsync(string clientCode, ICollection<string> beCodes = null)
        {
            var otbData = OtbBillingEntityClientData.Empty();

            if (UseApi)
            {
                ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(clientCode, nameof(clientCode));

                if (beCodes == null)
                {
                    beCodes = Array.Empty<string>();
                }

                try
                {
                    var response = await _otbApi.GetOtbClientBillingEntities(clientCode, beCodes);

                    if (response != null)
                    {
                        otbData = new OtbBillingEntityClientData
                        (
                            response.OriginCode ?? string.Empty,
                            response.OriginName ?? string.Empty,
                            response.ClientCode ?? string.Empty,
                            ParseOtbData(response)
                        );
                    }
                }
                catch
                {
                    _logger.LogInformation("OTB API unable to access Platform Configuration API.");
                }
            }

            return otbData;
        }

        /// <summary>
        /// Converts the list of billing entities and their fields to dictionaries of
        /// key value pairs and adds the origin and client data to the dictionary of fields.
        /// </summary>
        /// <returns>Each billing entity with the fields sorted in the following order:
        /// PayType, Origin, OriginName, TPA, all other fields sorted by the field order
        /// property (excluding the previous four and the line field) or an empty dictionary
        /// when there are no billing entities.</returns>
        private IDictionary<string, IDictionary<string, string>> ParseOtbData(OtbBillingEntityClientDto otbApiData)
        {
            var billingEntities = new Dictionary<string, IDictionary<string, string>>();
            var keyFields = new Dictionary<string, string>
            {
                { "Origin", otbApiData.OriginCode },
                { "OriginName", otbApiData.OriginName },
                { "TPA", otbApiData.ClientCode }
            };

            if (otbApiData.BillingEntities == null || !otbApiData.BillingEntities.Any())
            {
                return billingEntities;
            }

            foreach (var be in otbApiData.BillingEntities)
            {
                var fields = be.Fields.Where(f => f.FieldName == "PayType").ToDictionary(f => f.FieldName, f => f.FieldValue);
                var beCodeField = new Dictionary<string, string> { { "BillingEntity", be.BillingEntityCode } };
                var otherFields = be.Fields.OrderBy(f => f.FieldOrder).Where(f => f.FieldName != "00" && f.FieldName != "PayType").ToDictionary(f => f.FieldName, f => f.FieldValue);
                fields = fields.Concat(keyFields).Concat(beCodeField).Concat(otherFields).ToDictionary(x => x.Key, x => x.Value);
                billingEntities.Add(be.BillingEntityCode, fields);
            }

            return billingEntities;
        }
    }
}
