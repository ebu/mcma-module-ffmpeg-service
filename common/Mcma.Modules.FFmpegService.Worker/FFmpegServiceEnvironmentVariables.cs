using Mcma.Utility;

namespace Mcma.Modules.FFmpegService.Worker
{
    public static class FFmpegServiceEnvironmentVariables
    {
        public static string ExecutablePath => McmaEnvironmentVariables.Get("FFMPEG_EXECUTABLE_PATH", false);
        
        public static string OutputLocation => McmaEnvironmentVariables.Get("FFMPEG_OUTPUT_LOCATION", false);
    }
}