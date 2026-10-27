using VPay.Optum.PaymentRouting.RemittanceAdviceFile.Configurations;

namespace VPay.Optum.PaymentRouting.RemittanceAdviceFile.Consumers
{
    public static class Program
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
