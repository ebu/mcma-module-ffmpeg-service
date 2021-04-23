using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Mcma.Logging;
using Microsoft.Extensions.Options;

namespace Mcma.Modules.FFmpegService.Worker
{
    internal class FFmpegProcess : IFFmpegProcess
    {
        public FFmpegProcess(ILogger logger, IOptions<FFmpegOptions> options)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        private ILogger Logger { get; }
        
        private FFmpegOptions Options { get; }

        public async Task<(string stdOut, string stdErr)> RunAsync(params string[] args)
        {
            var processStartInfo = 
                new ProcessStartInfo(Options.ExecutablePath, string.Join(" ", args))
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

            using var process = Process.Start(processStartInfo);
            
            if (process == null)
                throw new Exception("Failed to start ffmpeg. Process.Start returned null.");
            
            Logger.Debug("FFmpeg process started. Reading stdout and stderr...");
            var stdOut = await process.StandardOutput.ReadToEndAsync();
            var stdErr = await process.StandardError.ReadToEndAsync();

            Logger.Debug("Waiting for FFmpeg process to exit...");
            process.WaitForExit();
            Logger.Debug($"FFmpeg process exited with code {process.ExitCode}.");

            if (process.ExitCode != 0)
                throw new Exception($"FFmpeg process exited with code {process.ExitCode}:\r\nStdOut:\r\n{stdOut}StdErr:\r\n{stdErr}");

            return (stdOut, stdErr);
        }
    }
}