using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VPay.Distribution.PmbQueueProcessor;
using VPay.Distribution.PmbQueueProcessor.Extensions;
using VPay.Extensions.Logging.GrayLog;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
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
            .ConfigureServices((context, services) =>
            {
                services
                    .AddOptions()
                    .AddAs400DataQueueService(context.Configuration)
                    .AddSingleton<MessageProcessor>()
                    .AddHostedService<QueueProcessorService>();
            })
            .Build()
            .RunAsync();
    }
}
