using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Gelf.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using VPay.Extensions.Logging.GrayLog;

namespace VPay.DocSys.Generation.Api
{
    public class Program
        {
            public static void Main(string[] args)
            {
                CreateHostBuilder(args).Build().Run();
            }
            public static IHostBuilder CreateHostBuilder(string[] args) =>
             Host.CreateDefaultBuilder(args)
              .UseServiceProviderFactory(new DefaultServiceProviderFactory())
              .BuildConfiguration()
              .UseLogging()
              .ConfigureWebHostDefaults(webBuilder =>
              {
                  webBuilder.UseStartup<Startup>();
              });
    }
}
