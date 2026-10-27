using Microsoft.Extensions.Hosting;
using VPay.Optum.Nc835FileProcessor.Consumer.Configurations;

namespace VPay.Optum.Nc835FileProcessor.Consumer
{
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
