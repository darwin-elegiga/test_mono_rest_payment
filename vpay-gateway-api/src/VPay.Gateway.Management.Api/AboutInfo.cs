namespace VPay.Gateway.Management.Api;

public class AboutInfo
{
    public string GitRevision { get; private set; }
    public string BuildTime { get; private set; }
    public string Environment { get; private set; }
    public string VersionInfo { get; private set; }
    public string DataCenter { get; private set; }

    public static AboutInfo GetBuildAboutInfo() =>
        new AboutInfo
        {
            GitRevision = System.Environment.GetEnvironmentVariable("RELEASE_COMMIT_SHA") ?? "[[GitRevision]]",
            BuildTime = System.Environment.GetEnvironmentVariable("IMAGE_BUILD_TIME") ?? "[[BuildTime]]",
            Environment = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
            VersionInfo = System.Environment.GetEnvironmentVariable("RELEASE_TAG") ?? "[[VersionInfo]]",
            DataCenter = System.Environment.GetEnvironmentVariable("DEPLOYMENT_DATACENTER") ?? "[[DataCenter]]"
        };
}
