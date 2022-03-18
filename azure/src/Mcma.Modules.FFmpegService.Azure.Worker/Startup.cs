using Mcma.Functions.Azure.Worker;
using Mcma.Model.Jobs;
using Mcma.Modules.FFmpegService.Azure.Worker;
using Mcma.Modules.FFmpegService.Worker;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mcma.Modules.FFmpegService.Azure.Worker
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
            => builder.Services
                      .AddFFmpeg<ConfigureFFmpegExecutablePath>()
                      .AddMcmaAzureFunctionJobAssignmentWorker<TransformJob>(
                          "ffmpeg-service-worker",
                          x => x.AddProfile<ExtractThumbnail>());
    }
}