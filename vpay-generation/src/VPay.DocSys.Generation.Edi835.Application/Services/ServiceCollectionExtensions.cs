using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Generation.Edi835.Application.PayFile;
using VPay.DocSys.Generation.Edi835.Application.Services;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835.Application
{
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// Adds <see cref="IEdi835CreationService"/> as a Transient scoped service for document creation
        /// </summary>
        /// <remarks>Requires entries for <seealso cref="Edi835ServiceSettings"/> and <seealso cref="IReconReportsApi">ReconReportsApiHttpClientConfig</seealso></remarks>
        public static IServiceCollection AddEdi835CreationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IEdi835CreationService, Edi835CreationService>();

            services.Configure<Edi835ServiceSettings>(configuration.GetSection("Edi835ServiceSettings"));
            services.AddTransient(cfg => cfg.GetService<IOptionsSnapshot<Edi835ServiceSettings>>().Value);

            services.TryAddTransient<IPayfileService, PayfileService>();
            services.TryAddTransient<ITrnService, TrnService>();

            services.Configure<ReconReportApiSettings>(configuration.GetSection("ReconReportsApiHttpClientConfig"));
            services.AddTransient(cfg => cfg.GetService<IOptionsSnapshot<ReconReportApiSettings>>().Value);

            var jitterer = new Random();

            services.AddHttpClient<IReconReportsApi, ReconReportsApi>(
                (options) =>
                {
                    options.BaseAddress = new Uri(configuration["ReconReportsApiHttpClientConfig:BaseAddress"]);
                    options.Timeout = TimeSpan.Parse(configuration["ReconReportsApiHttpClientConfig:Timeout"]);
                    var token = configuration["ReconReportsApiHttpClientConfig:ApiToken"];
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        options.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                }).AddPolicyHandler(
                    HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        3,
                        _ => TimeSpan.FromMilliseconds(300) + TimeSpan.FromMilliseconds(jitterer.Next(0, 100))));

            services.AddHttpClient<ILoafApi, LoafApi>(
                (options) =>
                {
                    options.BaseAddress = new Uri(configuration["LoafApiHttpClientConfig:BaseAddress"]);
                    options.Timeout = TimeSpan.Parse(configuration["LoafApiHttpClientConfig:Timeout"]);
                    var token = configuration["LoafApiHttpClientConfig:ApiToken"];
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        options.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                }).AddPolicyHandler(
                    HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        3,
                        _ => TimeSpan.FromMilliseconds(300) + TimeSpan.FromMilliseconds(jitterer.Next(0, 100))));

            services.AddSingleton<Edi835ConfigurationCache>();
            services.AddScoped<IDocsParserFactory, DocsParserFactory>();

            return services;
        }

    }
}
