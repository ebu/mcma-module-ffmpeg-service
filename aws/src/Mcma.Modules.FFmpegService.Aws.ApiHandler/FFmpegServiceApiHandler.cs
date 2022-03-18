using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Functions.Aws;
using Mcma.Functions.Aws.ApiHandler;
using Mcma.Serialization.Aws;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Modules.FFmpegService.Aws.ApiHandler
{
    public class FFmpegServiceApiHandler : McmaLambdaFunction<McmaLambdaApiHandler, APIGatewayProxyRequest, APIGatewayProxyResponse>
    {
        protected override void Configure(IServiceCollection services)
            => services.AddMcmaLambdaJobAssignmentApiHandler("ffmpeg-service-api-handler");
    }
}