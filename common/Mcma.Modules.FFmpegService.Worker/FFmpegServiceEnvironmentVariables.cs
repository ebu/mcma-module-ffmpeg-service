using Mcma.Utility;

namespace Mcma.Modules.FFmpegService.Worker
{
    public static class FFmpegServiceEnvironmentVariables
    {
        public static string DefaultOutputBucket => McmaEnvironmentVariables.Get("FFMPEG_DEFAULT_OUTPUT_BUCKET", false);
    }
}