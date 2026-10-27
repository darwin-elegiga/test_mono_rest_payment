using System;
using System.Globalization;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.Distribution
{
    public abstract class DistributionProcessor
    {
        protected readonly IFileSystem _fileSystem;
        protected readonly IDocSysHttpClient _docSysClient;
        protected readonly ApiSettings _apiSettings;
        protected readonly IConfiguration _configuration;
        protected readonly IDistributionRepository _distributionRepository;
        protected readonly ILogger _logger;

        protected DistributionProcessor(IConfiguration configuration, IFileSystem fileSystem, IDocSysHttpClient docSysClient, IDistributionRepository distributionRepository, ApiSettings apiSettings, ILogger logger)
        {
            _configuration = configuration;
            _fileSystem = fileSystem;
            _docSysClient = docSysClient;
            _distributionRepository = distributionRepository;
            _apiSettings = apiSettings;
            _logger = logger;
        }

        /// <summary>
        /// When implemented by a derived class, processes the distribution request.
        /// </summary>
        /// <param name="message">The distribution message.</param>
        /// <param name="createControlFile">The control file for the newly written .fax file</param>
        /// <returns>
        /// A <see cref="DistributionProcessResult"/> instance that contains information about the results of the process.
        /// </returns>
        public abstract Task<DistributionProcessResult> Process(DocDistMsg message, bool createControlFile = true);

        protected T GetConfigValue<T>(string key, DocSysConfiguration docSysConfig)
        {
            T result = default(T);
            string value = string.Empty;
            try
            {
                if (docSysConfig != null && docSysConfig.EnvironmentSettings.ContainsKey(key))
                {
                    value = docSysConfig.EnvironmentSettings[key];
                    result = (T)Convert.ChangeType(value, typeof(T));
                }
                else
                {
                    result = _configuration.GetSection("AppSettings").GetValue<T>(key);
                }
            }
            catch
            {
                //Could not convert.  Pass back default value...
                result = default(T);
            }

            return result;
        }

        protected string CreateControlFile(string sourceFilePath, string controlBase)
        {
            var filePath = _fileSystem.Path.GetDirectoryName(controlBase);
            var sourceFileName = _fileSystem.Path.GetFileName(sourceFilePath);
            _fileSystem.Directory.CreateDirectory(filePath);
            var controlFile = _fileSystem.Path.Combine(controlBase, sourceFileName + ".ctrl");
            _logger.LogInformation($"Writing control file: {controlFile}");
            var stream = _fileSystem.File.CreateText(controlFile);
            stream.WriteLine(sourceFilePath);
            stream.Flush();
            stream.Close();
            return controlFile;
        }

        internal string ConstructBasePath(DocDistMsg message)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(message.StatusFilePath, nameof(message), nameof(message.StatusFilePath));

            return _fileSystem.Path.GetDirectoryName(message.StatusFilePath);
        }
    }
}
