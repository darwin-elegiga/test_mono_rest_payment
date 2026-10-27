using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VPay.Extensions.Logging.GrayLog;

namespace VPay.DocSys.Generation.QueueProcessor.Consolidated
{
    class Program
    {
        public static IConfiguration Configuration { get; set; }

        static void Main(string[] args)
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
                 serviceCollection.AddServices(hostBuilderContext.Configuration);
                 serviceCollection.AddHostedService<QueueProcessorService>();
             });
    }
}
