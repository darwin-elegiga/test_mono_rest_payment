using System;
using System.Diagnostics.CodeAnalysis;

namespace VPay.Optum.PaymentRouting.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class AboutInfoHelper
    {
        public static string? EnvironmentName
        {
            get
            {
                string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                switch (env?.ToLower())
                {
                    case "local":
                    case "localdev":
                    case "localdevelopment":
                        return "LocalDevelopment";
                    case "dev":
                    case "development":
                        return "Development";
                    case "qa":
                        return "QA";
                    case "stage":
                    case "staging":
                        return "Staging";
                    case "prod":
                    case "production":
                        return "Production";
                    case "infra":
                    case "infrastructure":
                        return "Infrastructure";
                    default:
                        return env;
                }
            }
        }

        public static string? DataCenterName
        {
            get
            {
                string? env = Environment.GetEnvironmentVariable("DEPLOYMENT_DATACENTER");
                switch (env?.ToLower())
                {
                    case "pl":
                    case "plano":
                        return "Plano";
                    case "as":
                    case "austin":
                        return "Austin";
                    case "stl":
                    case "stlouis":
                        return "St. Louis";
                    default:
                        return env;
                }
            }
        }

        public static string? K8sNodeName => Environment.GetEnvironmentVariable("K8S_NODE_NAME");

        public static string? K8sPodName => Environment.GetEnvironmentVariable("K8S_POD_NAME");

        public static string? ReleaseTag => Environment.GetEnvironmentVariable("RELEASE_TAG");

        public static string? ReleaseCommitSHA => Environment.GetEnvironmentVariable("RELEASE_COMMIT_SHA");

        public static DateTime? ImageBuildTime => DateTime.Parse(GetBuildTime());

        public static DateTime? DeploymentTime => DateTime.Parse(GetDeployTime());

        private static string GetBuildTime()
        {
            var buildTime = Environment.GetEnvironmentVariable("IMAGE_BUILD_TIME");
            if (string.IsNullOrEmpty(buildTime))
            {
                buildTime = System.IO.File.GetLastWriteTimeUtc(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            return buildTime;
        }

        private static string GetDeployTime()
        {
            var deployTime = Environment.GetEnvironmentVariable("DEPLOYMENT_TIME");
            if (string.IsNullOrEmpty(deployTime))
            {
                deployTime = System.IO.File.GetLastWriteTimeUtc(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            return deployTime;
        }
    }
}
