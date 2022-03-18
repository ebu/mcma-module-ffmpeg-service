namespace Mcma.Modules.FFmpegService.Worker
{
    public class ExtractThumbnailOptions
    {
        public string TempOutputFolder { get; set; } = "/tmp";
        
        public string OutputLocation { get; set; } = FFmpegServiceEnvironmentVariables.OutputLocation;
    }
}