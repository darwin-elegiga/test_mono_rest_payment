using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GlobalExceptionHandler.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerUI;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing.Api
{
    public class Startup
    {
        private static string ApiTitle = "DocSys Parsing";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        private ILogger _logger;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddMvc(opt =>
            {
                opt.EnableEndpointRouting = false;
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
            .AddNewtonsoftJson();

            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });
            services.AddApiVersioning(o =>
            {
                o.ReportApiVersions = true;
                o.DefaultApiVersion = new ApiVersion(1, 0);
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.ApiVersionReader = new HeaderApiVersionReader();
            });

            services.AddDataProtection().DisableAutomaticKeyGeneration();
            services.ConfigureDependencies(Configuration);
            services.AddParsingHealthChecks(Configuration);
            services.AddSingleton<AboutInfo>();
            services.AddTransient<IArchiveRecordService, ArchiveRecordService>();

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                //Names used here are used in URL for SwaggerUI
                c.SwaggerDoc("v1.0", new OpenApiInfo { Title = $"{ApiTitle} API", Version = "v1.0" });

                //Determine which set of documentation an API should belong to
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var actionApiVersionModel = apiDesc.ActionDescriptor?.GetApiVersion();

                    // if no version is specified or API is marked version neutral add to all swagger documents
                    if (actionApiVersionModel == null || actionApiVersionModel.IsApiVersionNeutral)
                    {
                        return true;
                    }
                    if (actionApiVersionModel.DeclaredApiVersions.Any())
                    {
                        return actionApiVersionModel.DeclaredApiVersions.Any(v => $"v{v.ToString()}" == docName);
                    }
                    return actionApiVersionModel.ImplementedApiVersions.Any(v => $"v{v.ToString()}" == docName);
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            // NOTE: This sets up an authorization policy that allows all requests.
            // This is needed to make CodeQL happy about actions decored with certain attributes (like HttpDelete)
            services.AddAuthorization(options =>
                options.AddPolicy("AllowAll", policy => policy.RequireAssertion(_ => true)));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IHostApplicationLifetime appLifetime)
        {
            app.UseGlobalExceptionHandler(x =>
            {
                ConfigureExceptionHandler(x, env.IsDevelopment() || env.IsStaging());
            });

            app.UseMvc();
            app.UseSwagger();
            app.UseDeveloperExceptionPage();
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.DocExpansion(DocExpansion.None);
                c.DefaultModelRendering(ModelRendering.Model);
                c.SwaggerEndpoint("/swagger/v1.0/swagger.json", $"{ApiTitle} API");
                var appEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower();
                switch (appEnv)
                {
                    case "dev":
                    case "development":
                        c.DocumentTitle = $"{ApiTitle} API - DEV";
                        c.HeadContent = "<style>.swagger-ui {border:10px solid green}</style>";
                        break;
                    case "stg":
                    case "staging":
                        c.DocumentTitle = $"{ApiTitle} API - STG";
                        c.HeadContent = "<style>.swagger-ui {border:10px solid yellow}</style>";
                        break;
                    case "prod":
                    case "production":
                        c.DocumentTitle = $"{ApiTitle} API";
                        c.HeadContent = "<style>.swagger-ui {border:10px solid red}</style>";
                        break;
                }
            });
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.SetupRootHealthChecks();
                endpoints.SetupLivenessHealthChecks();
                endpoints.SetupStartupHealthChecks();
                endpoints.SetupReadinessHealthChecks();
            });

            _logger = loggerFactory.CreateLogger<Startup>();
            appLifetime.ApplicationStarted.Register(OnStartup);
            appLifetime.ApplicationStopping.Register(OnShutdown);
            appLifetime.ApplicationStopped.Register(OnShutdownComplete);
        }

        private void OnStartup()
        {
            _logger.LogInformation("Startup occurred for VPay.DocSys.Parsing.Api");
        }

        private void OnShutdown()
        {
            _logger.LogInformation("Shutdown occurred for VPay.DocSys.Parsing.Api");
        }

        private void OnShutdownComplete()
        {
            _logger.LogInformation("Shutdown completed for VPay.DocSys.Parsing.Api");
        }

        private void ConfigureExceptionHandler(ExceptionHandlerConfiguration config, bool useDetailedError)
        {
            config.ContentType = "application/json";

            if (useDetailedError)
            {
                config.ResponseBody(s => JsonConvert.SerializeObject(new
                {
                    message = s.Message,
                    exceptionType = s.GetType().FullName,
                    stacktrace = s.StackTrace,
                    innerException = s.InnerException == null ? null : new
                    {
                        message = s.InnerException.Message,
                        exceptionType = s.InnerException.GetType().FullName,
                        stackTrace = s.InnerException.StackTrace
                    }
                }));
            }
            else
            {
                config.ResponseBody(s => JsonConvert.SerializeObject(new
                {
                    message = "An error occurred while processing your request"
                }));
            }
        }
    }
}
