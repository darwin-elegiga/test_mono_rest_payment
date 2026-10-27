using System;

namespace VPay.DocSys.Generation.Api
{
    public class AboutInfo
    {
        public string BuildName { get { return "Docsys Generation API"; } }
        public string GitRevision { get; private set; }
        public string BuildTime { get; private set; }
        public string Environment { get; private set; }
        public string EnvironmentTime { get; private set; }
        public string VersionInfo { get; private set; }
        public string DataCenter { get; private set; }
        public string APIVersion { get; private set; }

        public AboutInfo(string apiVersion)
        {
            this.GitRevision = System.Environment.GetEnvironmentVariable("RELEASE_COMMIT_SHA") ?? "[[GitRevision]]";
            this.BuildTime = System.Environment.GetEnvironmentVariable("IMAGE_BUILD_TIME") ?? "[[BuildTime]]";
            this.Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            this.EnvironmentTime = DateTimeOffset.Now.ToString();
            this.VersionInfo = System.Environment.GetEnvironmentVariable("RELEASE_TAG") ?? "[[VersionInfo]]";
            this.DataCenter = System.Environment.GetEnvironmentVariable("DEPLOYMENT_DATACENTER") ?? "[[DataCenter]]";
            this.APIVersion = apiVersion;
        }
    }
}
