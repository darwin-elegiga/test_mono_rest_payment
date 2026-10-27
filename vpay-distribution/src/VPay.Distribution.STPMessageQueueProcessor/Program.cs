using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vpay.Distribution.STPMessageQueueProcessor;
using VPay.Extensions.Logging.GrayLog;

namespace VPay.Distribution.STPMessageQueueProcessor
{
    internal class Program
    {
        public static IConfiguration Configuration { get; set; }

        public static void Main(string[] args)
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
              .ConfigureServices((hostBuilderContext, serviceCollection) =>
              {
                  serviceCollection
                      .AddOptions()
                      .AddServices(hostBuilderContext);
              });
    }
}
