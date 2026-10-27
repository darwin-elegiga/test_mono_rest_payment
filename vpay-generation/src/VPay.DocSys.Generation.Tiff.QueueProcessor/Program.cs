using System;
using System.IO;
using Gelf.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VPay.DocSys.Generation.Extensions;
using VPay.Extensions.Logging.GrayLog;

namespace VPay.DocSys.Generation.Tiff.QueueProcessor
{
    public class Program
    {
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
                    serviceCollection
                        .AddOptions()
                        .AddServices(hostBuilderContext.Configuration)
                        .AddHostedService<TiffProcessorService>();
                });
    }
}
