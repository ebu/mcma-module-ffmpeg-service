using System;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Mcma.Client;
using Mcma.LocalFileSystem;
using Mcma.Modules.FFmpegService.Worker;
using Mcma.MongoDb;
using Mcma.Serilog;
using Mcma.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Mcma.Modules.FFmpegService.Generic.Worker
{
    public static class FFmpegServiceWorkerServiceCollectionExtensions
    {
        public static IServiceCollection AddFFmpegServiceWorker(this IServiceCollection services, IConfiguration configuration)
        {
            LocalFileSystemHelper.AddTypes();
            
            var mcmaConfig = configuration.GetSection("Mcma");

            var logger =
                new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

            services.AddHangfire(config =>
                    {
                        config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
                        config.UseSimpleAssemblyNameTypeSerializer();
                        config.UseRecommendedSerializerSettings();
                        config.UseFilter(new AutomaticRetryAttribute { Attempts = 0 });
                        config.UseMongoStorage(configuration.GetConnectionString("Hangfire"),
                                               new MongoStorageOptions
                                               {
                                                   QueuePollInterval = mcmaConfig.GetValue("QueuePollInterval", TimeSpan.FromSeconds(3)),
                                                   MigrationOptions = new MongoMigrationOptions { MigrationStrategy = new DropMongoMigrationStrategy() },
                                                   UseNotificationsCollection = false
                                               });
                    })
                    .AddHangfireServer(opts => opts.Queues = new[] { mcmaConfig["ServiceName"] });

            return services.AddMcmaSerilogLogging(mcmaConfig["ServiceName"], logger)
                           .AddMcmaMongoDb(opts => mcmaConfig.Bind("MongoDb", opts))
                           .AddMcmaClient(builder => builder.ConfigureDefaults(mcmaConfig["ServicesUrl"]))
                           .AddMcmaLocalFileSystemStorageClient()
                           .AddFFmpeg(@"C:\Program Files\ffmpeg\bin\ffmpeg.exe")
                           .AddMcmaWorker(
                               builder =>
                                   builder.AddProcessJobAssignmentOperation<TransformJob>(
                                       x =>
                                           x.AddProfile<ExtractThumbnail, ExtractThumbnailOptions>(
                                               opts => mcmaConfig.Bind("Profiles:ExtractThumbnail", opts))));
        }
    }
}