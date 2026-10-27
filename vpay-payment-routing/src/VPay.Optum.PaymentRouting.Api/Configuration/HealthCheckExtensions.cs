using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VPay.EdiFabric;

namespace VPay.Optum.PaymentRouting.Api.Configuration
{
    public static class HealthCheckExtensions
    {
        public static IHealthChecksBuilder AddEdiFabricHealthCheck(this IHealthChecksBuilder builder, string name = "EdiFabric")
        {
            return builder.AddCheck<EdiFabricHealthCheck>(name);
        }
    }

    public class EdiFabricHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Minimal valid 835 EDI content for testing
            const string Minimal835 = "ISA*00*          *00*          *ZZ*TEST           *ZZ*TEST           *210101*1253*^*00501*000000001*0*T*:~GS*HP*TEST*TEST*20210101*1253*1*X*005010X221A1~ST*835*0001~SE*2*0001~GE*1*1~IEA*1*000000001~";
            try
            {
                await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Minimal835));
                using var reader = new VPay835Reader(stream, false, true);
                var result = await reader.Read835Async().ConfigureAwait(false);

                return result != null
                    ? HealthCheckResult.Healthy("EdiFabric is operational.")
                    : HealthCheckResult.Unhealthy("EdiFabric failed to parse minimal 835.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("EdiFabric threw an exception during health check. Error: " + ex.Message + " Source: " + ex.Source + " Stack: " + ex.StackTrace);
            }
        }
    }    
}
