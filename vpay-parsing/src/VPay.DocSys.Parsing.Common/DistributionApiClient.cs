using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using EnumHelpers = VPay.DocSys.Parsing.Common.Extensions.EnumHelpers;

namespace VPay.DocSys.Parsing.Common
{
    public class DistributionApiClient : ClientApiBase, IDistributionApiClient
    {
        private readonly IFileSystem _fileSystem;
        private readonly HttpRequestPolicyHolder _httpRequestPolicies;

        public const string ServiceName = "DistributionApi";

        private enum EndPoint
        {
            Server
        }

        private readonly Dictionary<EndPoint, string> _endPoints;

        public DistributionApiClient(
            ApiSettings apiSettings,
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            ILogger<DistributionApiClient> logger)
            : base(httpClientFactory, logger, ServiceName)
        {
            ArgumentHelpers.ThrowIf.IsNull(apiSettings, nameof(apiSettings));
            ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));

            _fileSystem = fileSystem;

            _endPoints = EnumHelpers.GetEndPointDictionary<EndPoint>(apiSettings[ServiceName].EndPointFragments);

            _httpRequestPolicies = new HttpRequestPolicyHolder(logger);
        }

        public Task<IList<ServerFile>> FtpFiles(ScpSettings scpSettings, params string[] filePaths)
        {
            return this.FtpFiles(scpSettings, true, filePaths);
        }

        public async Task<IList<ServerFile>> FtpFiles(ScpSettings scpSettings, bool createCtrlFiles, params string[] filePaths)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(filePaths, nameof(filePaths));
            ArgumentHelpers.ThrowIf.IsNull(scpSettings, nameof(scpSettings));

            var files = filePaths
                .Select(_ => new ServerFile
                {
                    FilePath = _,
                    ControlFileName = $"{_fileSystem.Path.GetFileName(_)}.ctrl"
                })
                .ToList();

            var content = new ServerParams
            {
                FtpFiles = files,
                ScpSettings = scpSettings,
                CreateCtrlFiles = createCtrlFiles,
            };

            return await _httpRequestPolicies.HttpPostPolicy.ExecuteAsync(() =>
            {
                return Post<List<ServerFile>>(_endPoints[EndPoint.Server], content.ToJsonStringContent());

            }, this.GetUri(_endPoints[EndPoint.Server]).ToString());
        }
    }
}
