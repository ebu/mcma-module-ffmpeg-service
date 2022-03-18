using Amazon.Lambda.Core;
using Mcma.Functions.Aws;
using Mcma.Functions.Aws.Worker;
using Mcma.Model.Jobs;
using Mcma.Modules.FFmpegService.Worker;
using Mcma.Serialization.Aws;
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
