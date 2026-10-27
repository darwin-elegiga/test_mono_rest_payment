using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using VPay.Optum.PaymentRouting.ApProcessor.Configurations;

namespace VPay.Optum.PaymentRouting.ApProcessor
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .BuildConfiguration()
                .UseLogging()
                .ConfigureServices(Startup.ConfigureServices);
    }
}
