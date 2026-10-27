using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Parsing
{
    public class XmlConfigParserService : IXmlConfigParserService
    {
        private static XmlSerializer _xmlSerializer = new XmlSerializer(typeof(ConfigurationXmlDto));

        private readonly IDocSysConfigService _docSysConfigService;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<XmlConfigParserService> _logger;

        public XmlConfigParserService(
            IDocSysConfigService docSysConfigService,
            IFileSystem fileSystem,
            ILogger<XmlConfigParserService> logger)
        {
            ThrowIf.IsNull(docSysConfigService, nameof(docSysConfigService));
            ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ThrowIf.IsNull(logger, nameof(logger));

            _docSysConfigService = docSysConfigService;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public ConfigurationXmlDto ReadXmlConfiguration(string tpaCode, string beCode)
        {
            using (_logger.BeginScope(new { TPA = tpaCode, BE = beCode, BillingEntity = beCode }.ToDictionary()))
            {
                ThrowIf.IsNullOrWhiteSpace(tpaCode, nameof(tpaCode));
                ThrowIf.IsNullOrWhiteSpace(beCode, nameof(beCode));

                var appSettings = GetApplicationSettings(tpaCode);

                var filePaths = GetFileList(appSettings, beCode);

                var configuration = ReadAndConsolidateConfigurations(filePaths);

                return configuration;
            }
        }

        private Dictionary<string, string> GetApplicationSettings(string tpaCode)
        {
            try
            {
                var config = _docSysConfigService.ReadConfig(tpaCode);

                return config.AppSettings;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not read configuration for TPA:{TPA}", tpaCode.Replace(Environment.NewLine, ""));
                throw;
            }
        }

        private IList<string> GetFileList(Dictionary<string, string> appSettings, string beCode)
        {
            // get required configuration list
            var fileKeys = appSettings["required_vpayge_config_list"]
                .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(_ => _.Trim())
                .ToList();

            // add all valid be code key variations
            fileKeys.AddRange(GetBeFileKeys(beCode).Intersect(appSettings.Keys));

            var paths = fileKeys
                .Distinct()
                .Select(_ => appSettings[_])
                .ToList();

            ValidateFilePaths(paths);

            return paths;
        }

        private IEnumerable<string> GetBeFileKeys(string beCode)
        {
            // remove all digits
            var tpaIndex = Regex.Replace(beCode, @"\d", "").Length;

            // add both upper and lower case
            for (int i = tpaIndex; i < beCode.Length + 1; i++)
            {
                var key = beCode.Substring(0, i);
                var lkey = key.ToLower();

                _logger.LogInformation("Add {INIKey}", key.Replace(Environment.NewLine, ""));
                yield return $"vpayge_config_{key}";

                if (key != lkey)
                {
                    _logger.LogInformation("Add {INIKey}", lkey.Replace(Environment.NewLine, ""));
                    yield return $"vpayge_config_{lkey}";
                }
            }
        }

        private void ValidateFilePaths(IList<string> paths)
        {
            foreach (var p in paths)
            {
                if (!_fileSystem.File.Exists(p))
                {
                    _logger.LogError("Configuration file not found: {FilePath}", p);

                    throw new FileNotFoundException("Configuration file not found", p);
                }
            }
        }

        private ConfigurationXmlDto ReadAndConsolidateConfigurations(IEnumerable<string> filePaths)
        {
            var configurations = filePaths
                .Select(_ =>
                {
                    using (var fs = _fileSystem.File.OpenRead(_))
                    {
                        _logger.LogInformation("Read configuration file: {FilePath}", _);

                        return (ConfigurationXmlDto)_xmlSerializer.Deserialize(fs);
                    }
                });

            return new ConfigurationXmlDto
            {
                Common = configurations.SelectMany(_ => _.Common).ConsolidateFields(),
                Check = configurations.SelectMany(_ => _.Check).ConsolidateFields(),
                CLCCK = configurations.SelectMany(_ => _.CLCCK).ConsolidateFields(),
                CLCCP = configurations.SelectMany(_ => _.CLCCP).ConsolidateFields(),
                CLCFT = configurations.SelectMany(_ => _.CLCFT).ConsolidateFields(),
                CLVCH = configurations.SelectMany(_ => _.CLVCH).ConsolidateFields(),
                EFT = configurations.SelectMany(_ => _.EFT).ConsolidateFields(),
                NPF = configurations.SelectMany(_ => _.NPF).ConsolidateFields(),
                VCard = configurations.SelectMany(_ => _.VCard).ConsolidateFields(),
            };
        }
    }
}
