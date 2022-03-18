using Mcma.Client;
using Mcma.Data.MongoDB;
using Mcma.Logging.Serilog;
using Mcma.Model.Jobs;
using Mcma.Modules.FFmpegService.Worker;
using Mcma.Storage.LocalFileSystem;
using Mcma.Worker;
using Mcma.Worker.Jobs;
using Mcma.Worker.Kafka;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Mcma.Modules.FFmpegService.Kubernetes.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) => 
                {
                    LocalFileSystemHelper.AddTypes();

                    services.AddMcmaSerilogLogging(loggerConfig => loggerConfig.WriteTo.Console(), "ffmpeg-service-worker")
                            .AddMcmaMongoDb()
                            .AddMcmaClient()
                            .AddMcmaLocalFileSystemStorageClient()
                            .AddFFmpeg()
                            .AddMcmaWorker(builder => builder.AddProcessJobAssignmentOperation<TransformJob>(x => x.AddProfile<ExtractThumbnail>()))
                            .AddMcmaKafkaWorkerService();
                });
    }
}
