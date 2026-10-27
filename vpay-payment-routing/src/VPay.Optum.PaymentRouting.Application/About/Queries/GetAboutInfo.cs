using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VPay.Optum.PaymentRouting.Common.Helpers;

namespace VPay.Optum.PaymentRouting.Application.About.Queries
{
    public class GetAboutInfo
    {
        public record Query : IRequest<AboutInfo> { }

        public class AboutInfo
        {
            public string Application { get; set; } = default!;
            public string Environment { get; set; } = default!;
            public string DataCenter { get; set; } = default!;
            public string K8sNodeName { get; set; } = default!;
            public string K8sPodName { get; set; } = default!;
            public string ReleaseTag { get; set; } = default!;
            public string ReleaseCommitSHA { get; set; } = default!;
            public DateTime? ImageBuildTime { get; set; }
            public DateTime? DeploymentTime { get; set; }
        }

        public class Handler : IRequestHandler<Query, AboutInfo>
        {
            public Task<AboutInfo> Handle(Query request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new AboutInfo
                {
                    Application = "VPay Optum Payment Routing API",
                    Environment = AboutInfoHelper.EnvironmentName ?? string.Empty,
                    DataCenter = AboutInfoHelper.DataCenterName ?? string.Empty,
                    K8sNodeName = AboutInfoHelper.K8sNodeName ?? string.Empty,
                    K8sPodName = AboutInfoHelper.K8sPodName ?? string.Empty,
                    ReleaseTag = AboutInfoHelper.ReleaseTag ?? string.Empty,
                    ReleaseCommitSHA = AboutInfoHelper.ReleaseCommitSHA ?? string.Empty,
                    ImageBuildTime = AboutInfoHelper.ImageBuildTime,
                    DeploymentTime = AboutInfoHelper.DeploymentTime
                });
            }
        }
    }
}
