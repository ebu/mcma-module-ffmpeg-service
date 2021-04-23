using System.Threading.Tasks;

namespace Mcma.Modules.FFmpegService.Worker
{
    public interface IFFmpegProcess
    {
        Task<(string stdOut, string stdErr)> RunAsync(params string[] args);
    }
}