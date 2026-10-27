using System;
using Microsoft.Extensions.Configuration;

namespace VPay.Distribution.Api
{
    public class AboutInfo
    {
        public string BuildName { get { return "Docsys Distribution API"; } }
        public string GitRevision { get; private set; }
        public string BuildTime { get; private set; }
        public string Environment { get; private set; }
        public string VersionInfo { get; private set; }
        public string DataCenter { get; private set; }

        public AboutInfo()
        {
            this.GitRevision = System.Environment.GetEnvironmentVariable("RELEASE_COMMIT_SHA") ?? "[[GitRevision]]";
            this.BuildTime = System.Environment.GetEnvironmentVariable("IMAGE_BUILD_TIME") ?? "[[BuildTime]]";
            this.Environment = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            this.VersionInfo = System.Environment.GetEnvironmentVariable("RELEASE_TAG") ?? "[[VersionInfo]]";
            this.DataCenter = System.Environment.GetEnvironmentVariable("DEPLOYMENT_DATACENTER") ?? "[[DataCenter]]";
        }
    }
}
