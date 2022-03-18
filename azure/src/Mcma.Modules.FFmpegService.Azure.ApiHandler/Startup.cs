using Mcma.Functions.Azure.ApiHandler;
using Mcma.Modules.FFmpegService.Azure.ApiHandler;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mcma.Modules.FFmpegService.Azure.ApiHandler
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
            => builder.Services.AddMcmaAzureFunctionJobAssignmentApiHandler("ffmpeg-service-api-handler");
    }
}