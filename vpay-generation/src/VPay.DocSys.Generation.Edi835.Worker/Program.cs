using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VPay.Extensions.Logging.GrayLog;

namespace VPay.DocSys.Generation.Edi835.Worker
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((config) =>
                {
                    config
                        .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
                        .AddJsonFile("appsettings.secure.json", optional: true, reloadOnChange: true);
                })
                .ConfigureLogging(logging =>
                {
                    logging
                        .AddVPayGrayLog();
                })
                .ConfigureServices(Startup.ConfigureServices);
    }
}
