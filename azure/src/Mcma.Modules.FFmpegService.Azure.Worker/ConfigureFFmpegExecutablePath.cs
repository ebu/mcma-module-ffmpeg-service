using System.IO;
using Mcma.Modules.FFmpegService.Worker;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Options;

namespace Mcma.Modules.FFmpegService.Azure.Worker
{
    public class ConfigureFFmpegExecutablePath : IConfigureOptions<FFmpegOptions>
    {
        public ConfigureFFmpegExecutablePath(IOptions<ExecutionContextOptions> executionContextOptions)
            => HostRootDir = executionContextOptions.Value?.AppDirectory;
        
        private string HostRootDir { get; }

        public void Configure(FFmpegOptions options)
            => options.ExecutablePath = Path.Combine(HostRootDir, "exe", "ffmpeg.exe");
    }
}