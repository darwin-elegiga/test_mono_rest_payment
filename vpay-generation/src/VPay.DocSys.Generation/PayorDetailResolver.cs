using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Storage;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class PayorDetailResolver : IPayorDetailResolver
    {
        /// <summary>
        /// The starting <see cref="FieldConfiguration.Name"/> of finding The VCard Payor name
        /// </summary>
        public static readonly string PayorLookupName = "body1";

        /// <summary>
        /// Regular expression to search in the <see cref="FieldConfiguration.Value"/>
        /// to find the next <see cref="FieldConfiguration"/> 
        /// </summary>
        public static readonly Regex PayorLookupValueRegex = new Regex(
            @"\${(\S*)}.* is expediting|on behalf of \${(\S*)}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IArchiveService _archiveService;
        private readonly ILogger<PayorDetailResolver> _logger;

        /// <summary>
        /// Regular Expression to find the variable in <see cref="FieldConfiguration.Value"/>
        /// to match with <see cref="FieldConfiguration.Name"/>
        /// </summary>
        private static readonly Regex VariableRegex = new Regex(@"\${(\S*)}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public PayorDetailResolver(
            IArchiveService archiveService,
            ILogger<PayorDetailResolver> logger)
        {
            ThrowIf.IsNull(archiveService, nameof(archiveService));
            ThrowIf.IsNull(logger, nameof(logger));

            _archiveService = archiveService;
            _logger = logger;
        }

        public async Task<PayorDetail> ResolvePayorDetails(
            ClientType clientType,
            string tpaCode,
            string beCode,
            long transactionId,
            OtbDetailResponse otbDetailResponse,
            FieldDictionary configurationDictionary)
        {
            ThrowIf.IsNullOrWhiteSpace(tpaCode, nameof(tpaCode));
            ThrowIf.IsNullOrWhiteSpace(beCode, nameof(beCode));
            ThrowIf.IsNull(otbDetailResponse, nameof(otbDetailResponse));

            if (clientType == ClientType.MEDICAL)
            {
                ThrowIf.IsNull(configurationDictionary, nameof(configurationDictionary));
            }

            using (_logger.BeginScope(otbDetailResponse.ToDictionary()))
            using (_logger.BeginScope(new
            {
                ClientType = clientType.ToString(),
                BE = beCode,
                BillingEntity = beCode,
                TPA = tpaCode,
                ClientCode = tpaCode,
                TransactionId = transactionId,
                SETransactionId = transactionId
            }.ToDictionary()))
            {
                string payorName = null;
                bool isEobDependent = false;

                switch (clientType)
                {
                    case ClientType.WARRANTY:
                        payorName = otbDetailResponse.ClientFullName;

                        _logger.LogInformation("Set PayorName:{PayorName} for ClientType:{ClientType}", payorName, clientType.ToString().Replace(Environment.NewLine, ""));
                        break;

                    case ClientType.MEDICAL:
                        var payorVariable = FindVCardPayorVariable(configurationDictionary);

                        if (payorVariable != null)
                        {
                            (payorName, isEobDependent) = await ResolveField(
                                configurationDictionary,
                                payorVariable,
                                transactionId);

                            if (payorName != null)
                            {
                                _logger.LogInformation("Resolved Contact Name, Using {PayorName}", payorName);
                            }
                            else
                            {
                                _logger.LogWarning("Could not resolve Contact Name");
                            }
                        }

                        if (payorName == null)
                        {
                            _logger.LogInformation("Using default value: {PayorName}", payorName);

                            payorName = otbDetailResponse.ClientFullName;
                        }
                        break;

                    default:
                        _logger.LogError("Invalid ClientType:{ClientType}", clientType.ToString().Replace(Environment.NewLine, ""));

                        throw new ArgumentException($"Invalid ClientType:{clientType}");
                }

                return new PayorDetail
                {
                    PayorName = payorName,
                    VPayPhone = otbDetailResponse.VPayPhone,
                    PayorPhone = otbDetailResponse.ClientPhone,
                    IsEobDependent = isEobDependent
                };
            }
        }

        public FieldDictionary CreateFieldDictionary(OtbDetailResponse otbDetail, ClientVPaygeSettingsResponse vpaygeSettings)
        {
            // create OTB entry dictionary
            var otbFields = new OtbEntry(otbDetail)
                .ToDictionary()
                .Select(_ => new FieldConfiguration
                {
                    Name = $"OTB_ENTRY.{_.Key}",
                    Value = (string)_.Value,
                    Source = FieldConfiguration.SourceEnum.Literal,
                    Type = FieldConfiguration.TypeEnum.String,
                });

            _logger.LogInformation("Create OTB_Entry Dictionary");

            var dictionary = (vpaygeSettings.Common ?? Array.Empty<FieldConfiguration>())
                .Union(vpaygeSettings.VCard ?? Array.Empty<FieldConfiguration>())
                .Union(otbFields)
                .GroupBy(_ => _.Name)
                .ToDictionary(k => k.Key, v => v.Last());

            return new FieldDictionary(dictionary);
        }

        private string FindVCardPayorVariable(IDictionary<string, FieldConfiguration> fields)
        {
            // always starts in body1
            if (fields.TryGetValue(PayorLookupName, out var field) &&
                field.Source == FieldConfiguration.SourceEnum.Variable &&
                field.Type == FieldConfiguration.TypeEnum.String)
            {
                _logger.LogInformation("Found '{Name}' Field Configuration", field.Name);

                // always surrounded by certain text
                var key = PayorLookupValueRegex.Matches(field.Value);

                return key.GetLastSuccess();
            }
            // exit early if starting field not found 
            else if (field == null)
            {
                _logger.LogWarning("No Field with Name:{Name} found", PayorLookupName);
            }
            else
            {
                using (_logger.BeginScope(field.ToDictionary()))
                {
                    _logger.LogWarning("Invalid Field for Name:{Name} found", PayorLookupName);
                }
            }

            return null;
        }

        private async Task<(string, bool)> ResolveField(FieldDictionary fields, string key, long transactionId)
        {
            if (fields.TryGetValue(key, out var field))
            {
                while (field.Source == FieldConfiguration.SourceEnum.Variable
                    && field.Type == FieldConfiguration.TypeEnum.String
                    //  stop resolving at non-configuration variable
                    && !field.Value.Contains("."))
                {
                    using (_logger.BeginScope(field.ToDictionary()))
                    {
                        field = LookupField(fields, field);

                        if (field == null) { return (null, false); }
                    }
                }
            }
            else if (key.Contains(".")) // key is not in dictionary, but can be for 'external data'
            {
                field = new FieldConfiguration
                {
                    Value = key
                };
            }
            else // key lookup failed
            {
                _logger.LogWarning("No Field with Name:{Name} found", key);

                return (null, false);
            }

            // early exit
            if (field.Source == FieldConfiguration.SourceEnum.Literal &&
                field.Type == FieldConfiguration.TypeEnum.String)
            {
                return (field.Value, false);
            }

            if (field.Value.Contains("."))
            {
                //clean up, just it case
                var match = VariableRegex.Match(field.Value);

                if (match.Success) { field.Value = match.GetLastSuccess(); }

                if (field.Value.StartsWith("eob_xml_dict"))
                {
                    // can directly search, do not need to create a dictionary
                    var result = await SearchEobXml(transactionId, field.Value);

                    return (result, true);
                }
                else // OTB_ENTRY fields already added
                {
                    field = LookupField(fields, field);

                    if (field == null) { return (null, false); }
                }
            }

            if (field.Source != FieldConfiguration.SourceEnum.Literal)
            {
                _logger.LogError("Cannot resolve Source:{Source} for Field:{Name}", field.Source, field.Name);

                return (null, false);
            }
            else if (field.Type != FieldConfiguration.TypeEnum.String)
            {
                _logger.LogError("Cannot resolve Type:{Type} for Field:{Name}", field.Type, field.Name);

                return (null, false);
            }

            return (field.Value, false);
        }

        private FieldConfiguration LookupField(IDictionary<string, FieldConfiguration> fields, FieldConfiguration field)
        {
            var match = VariableRegex.Match(field.Value);
            var value = match.GetLastSuccess();

            if (match.Success && fields.TryGetValue(value, out var nextField))
            {
                _logger.LogInformation("Resolve {Name} to {NewName}", field.Name, nextField.Name);

                return nextField;
            }
            else if (fields.TryGetValue(field.Value, out nextField))
            {
                _logger.LogInformation("Resolve {Value} to {NewName}", field.Value, nextField.Name);

                return nextField;
            }
            else if (nextField == null)
            {
                _logger.LogWarning("No Lookup Field found for Field Value '{Value}' as key", field.Value);

                return null;
            }
            else if (nextField.Name == field.Name)
            {
                _logger.LogWarning("Could not resolve {Name}:{Value} Field", field.Name, field.Value);

                return null;
            }

            return null;
        }

        private async Task<string> SearchEobXml(long transactionId, string value)
        {
            var xPath = value
                .Replace("eob_xml_dict", "./eob_xml") // use correct starting node name
                .Replace(".", "/"); // convert to xpath

            var eobXml = await _archiveService.GetArchiveDetailForTransactionIDAndFileName(transactionId, "eob.xml");

            if (eobXml == null)
            {
                _logger.LogWarning("File Ending in 'eob.xml' not found");

                return null;
            }

            using (var ms = new MemoryStream(eobXml.Data))
            {
                try
                {
                    var xdoc = XDocument.Load(ms);

                    var element = xdoc.XPathSelectElement(xPath);

                    return element?.Value;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Could not resolve XPath:{XPath} from Value:{Value}", xPath, value);
                }

                return null;
            }
        }
    }
}
