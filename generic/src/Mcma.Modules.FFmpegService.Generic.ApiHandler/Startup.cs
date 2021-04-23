using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Mcma.Api;
using Mcma.Api.Routing.Defaults;
using Mcma.AspNetCore;
using Mcma.Client;
using Mcma.HangfireWorkerInvoker;
using Mcma.LocalFileSystem;
using Mcma.MongoDb;
using Mcma.Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Mcma.Modules.FFmpegService.Generic.ApiHandler
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            LocalFileSystemHelper.AddTypes();

            var mcmaConfig = Configuration.GetSection("Mcma");
            
            var logger =
                new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

            services.AddMcmaSerilogLogging(mcmaConfig["ServiceName"], logger);
            
            services.AddMcmaMongoDb(opts => mcmaConfig.Bind("MongoDb", opts));

            services.AddMcmaHangfireWorkerInvoker(mcmaConfig["ServiceName"],config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
                config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();
                config.UseMongoStorage(Configuration.GetConnectionString("Hangfire"),
                                       new MongoStorageOptions
                                       {
                                           MigrationOptions = new MongoMigrationOptions {MigrationStrategy = new DropMongoMigrationStrategy()}
                                       });
            });

            services.AddMcmaClient(builder => builder.ConfigureDefaults(mcmaConfig["ServicesUrl"]))
                    .AddMcmaApi(builder =>
                                    builder.Configure(opts => opts.PublicUrl = "http://localhost:36360")
                                           .AddDefaultJobAssignmentRoutes());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseMcmaApiHandler();
        }
    }
}
