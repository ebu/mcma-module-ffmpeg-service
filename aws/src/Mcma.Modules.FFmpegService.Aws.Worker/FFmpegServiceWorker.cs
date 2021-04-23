using Amazon.Lambda.Core;
using Mcma.Aws.Functions;
using Mcma.Aws.Functions.ApiHandler;
using Mcma.Aws.Functions.Worker;
using Mcma.Aws.Lambda;
using Mcma.Modules.FFmpegService.Worker;
using Mcma.Worker;
using Mcma.Worker.Common;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Modules.FFmpegService.Aws.Worker
{
    public class FFmpegServiceWorker : McmaLambdaFunction<McmaLambdaWorker, McmaWorkerRequest>
    {
        protected override void Configure(IServiceCollection services)
            => services
               .AddFFmpeg("/opt/bin/ffmpeg")
               .AddMcmaAwsLambdaJobAssignmentWorker<TransformJob>(
                   "ffmpeg-service-worker",
                   builder => builder.AddProfile<ExtractThumbnail>());
    }
}
