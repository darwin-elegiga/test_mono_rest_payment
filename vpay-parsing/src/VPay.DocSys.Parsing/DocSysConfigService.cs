using System;
using IniParser;
using IniParser.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;

namespace VPay.DocSys.Parsing
{
    public class DocSysConfigService : IDocSysConfigService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        private readonly string _basePath;
        public DocSysConfigService(IConfiguration configuration, ILogger<DocSysConfigService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _basePath = _configuration.GetSection("StatusFile").GetValue<string>("PythonHome");

        }

        /// <summary>
        /// Gather system and TPA-specific configuration information
        /// </summary>
        /// <param name="tpa">Three letter code for the Third Party Administrator whose configuration we want</param>
        /// <returns></returns>
        public DocSysConfiguration ReadConfig(string tpa)
        {
            DocSysConfiguration docSysConfig = new DocSysConfiguration();
            Dictionary<string, string> envConfig = new Dictionary<string, string>();
            Dictionary<string, string> appConfig = new Dictionary<string, string>();
            var parser = new FileIniDataParser();
            try
            {
                using (_logger.BeginScope(new Dictionary<string, object>()
                {
                    { "TPA", tpa }
                }))
                {
                    string eobMergeIni = _configuration.GetSection("StatusFile").GetValue<string>("EobMergeConfig");
                    appConfig = ReadConfigSettings(appConfig, eobMergeIni);

                    //Get the system level settings 
                    string eobSettingsIni = _configuration.GetSection("StatusFile").GetValue<string>("EobMergeSettings");
                    envConfig = ReadEnvironmentSettings(envConfig, eobSettingsIni);

                    //TPA specific settings will override system level
                    //Get the settings from the TPA.ini
                    var tpaSmbSafeName = tpa.EnsureIsSmbSafeName();
                    string tpaIniFile = _basePath + tpaSmbSafeName + "/" + tpaSmbSafeName + ".ini";
                    envConfig = ReadEnvironmentSettings(envConfig, tpaIniFile);

                    if (appConfig.Keys.Contains("tpa_config_file"))
                    {
                        appConfig = ReadConfigSettings(appConfig, appConfig["tpa_config_file"]);
                    }

                    if (envConfig.TryGetValue("tpa_config_file", out var tpaConfigFile))
                    {
                        appConfig = ReadConfigSettings(appConfig, tpaConfigFile);
                    }

                    docSysConfig.AppSettings = appConfig;
                    docSysConfig.EnvironmentSettings = envConfig;
                    return docSysConfig;
                }
            }
            catch (Exception ex) when (LogError(ex))
            {
                throw;
            }
        }

        bool LogError(Exception ex)
        {
            _logger.LogError(ex, "An unexpected exception occurred");
            return true;
        }

        /// <summary>
        /// Read files designed to set local environment variables.
        /// </summary>
        /// <param name="existingConfig">Pass in existing values. Allows for cascading config settings System/TPA/BE etc.</param>
        /// <param name="fileName">Name of the file with where environment variables are set.</param>
        /// <returns></returns>
        private Dictionary<string, string> ReadEnvironmentSettings(Dictionary<string, string> existingConfig, string fileName)
        {
            var commentRegex = new System.Text.RegularExpressions.Regex("^[;#].*");
            fileName = fileName.ToLower();
            if (!File.Exists(fileName))
            {
                _logger.LogWarning("Could not find config file [{FileName}]", fileName.Replace(Environment.NewLine, ""));
                return existingConfig;
            }

            _logger.LogInformation("Started parsing config file [{FileName}]", fileName.Replace(Environment.NewLine, ""));

            using var configFile = new StreamReader(fileName);
            string line;
            while ((line = configFile.ReadLine()) != null)
            {
                line = line.Trim();
                if (commentRegex.IsMatch(line) || string.IsNullOrEmpty(line))
                {
                    continue;
                }
                var elements = line.Split('=');
                int separationPoint = line.IndexOf("=");
                var cfgVariable = line.Substring(0, separationPoint).Replace("export ", string.Empty);
                var cfgValue = line.Substring(separationPoint + 1);
                if (cfgValue.StartsWith("\"\"\"") || cfgValue.StartsWith("'''"))
                {
                    string subsequentLine;
                    cfgValue = cfgValue.Substring(2);
                    while ((subsequentLine = configFile.ReadLine()) != null)
                    {
                        cfgValue += " " + subsequentLine;
                        if (subsequentLine.EndsWith("\"\"\"") || subsequentLine.EndsWith("'''"))
                        {
                            cfgValue = cfgValue.Substring(0, cfgValue.Length - 2);
                            break;
                        }
                    }
                }
                if (cfgValue.StartsWith("\"") && cfgValue.EndsWith("\"") ||
                    cfgValue.StartsWith("'") && cfgValue.EndsWith("'"))
                {
                    cfgValue = cfgValue.Substring(1, cfgValue.Length - 2);
                }

                if (existingConfig.Keys.Contains(cfgVariable))
                {
                    _logger.LogInformation("Replacing {OldConfigValue} with {ConfigValue} for Key:{ConfigKey}",
                        existingConfig[cfgVariable].Replace(Environment.NewLine, ""),
                        cfgVariable.Replace(Environment.NewLine, ""),
                        cfgVariable.Replace(Environment.NewLine, ""));

                    existingConfig[cfgVariable] = cfgValue;
                }
                else
                {
                    existingConfig.Add(cfgVariable, cfgValue);
                }

            }

            _logger.LogInformation("Completed parsing config file [{FileName}]", fileName.Replace(Environment.NewLine, ""));

            return existingConfig;
        }

        /// <summary>
        /// Get application settings from ini files.
        /// </summary>
        /// <param name="existingConfig">Pass in existing values. Allows for cascading config settings System/TPA/BE etc.</param>
        /// <param name="fileName">Name of the file with application settings</param>
        /// <returns></returns>
        private Dictionary<string, string> ReadConfigSettings(Dictionary<string, string> existingConfig, string fileName)
        {
            var parser = new FileIniDataParser();
            parser.Parser.Configuration.CommentRegex = new System.Text.RegularExpressions.Regex("^[;#].*");
            parser.Parser.Configuration.NewLineStr = "\n";
            parser.Parser.Configuration.AllowKeysWithoutSection = true;

            _logger.LogInformation("Started parsing config file [{FileName}]", fileName);

            //Get more configuration from TPA config.ini
            IniData tpaConfigData = parser.ReadFile(fileName);
            string serverType = string.Empty;
            foreach (var section in tpaConfigData.Sections)
            {
                foreach (var cfgLine in section.Keys)
                {
                    if (cfgLine.KeyName == "server_type")
                    {
                        serverType = cfgLine.Value;
                    }
                    //Skip configuration that isn't for the current environment
                    if (!string.IsNullOrEmpty(serverType) && cfgLine.KeyName.Contains('.') && !cfgLine.KeyName.StartsWith(serverType))
                    { continue; }
                    string configKey = cfgLine.KeyName.Replace(serverType + ".", string.Empty);
                    if (cfgLine.Value.StartsWith("\"") && cfgLine.Value.EndsWith("\"") ||
                    cfgLine.Value.StartsWith("'") && cfgLine.Value.EndsWith("'"))
                    {
                        cfgLine.Value = cfgLine.Value.Substring(1, cfgLine.Value.Length - 2);
                    }
                    if (existingConfig.Keys.Contains(configKey))
                    {
                        _logger.LogInformation("Replacing {OldConfigValue} with {ConfigValue} for Key:{ConfigKey}", existingConfig[configKey], cfgLine.Value, configKey);
                        existingConfig[configKey] = cfgLine.Value;
                    }
                    else
                    {
                        existingConfig.Add(configKey, cfgLine.Value);
                    }
                }
            }

            _logger.LogInformation("Completed parsing config file [{FileName}]", fileName);

            return existingConfig;
        }
    }
}
