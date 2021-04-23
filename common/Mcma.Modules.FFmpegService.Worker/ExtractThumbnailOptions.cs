namespace Mcma.Modules.FFmpegService.Worker
{
    public class ExtractThumbnailOptions
    {
        public string OutputLocation { get; set; } = FFmpegServiceEnvironmentVariables.DefaultOutputBucket;
    }
}