using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Serialization;
using Mcma.Storage;
using Mcma.Worker;

namespace Mcma.Modules.FFmpegService.Worker
{
    public class ExtractThumbnail : IJobProfile<TransformJob>
    {
        public ExtractThumbnail(IStorageClient storageClient, IFFmpegProcess ffmpegProcess, IHttpClientFactory httpClientFactory)
        {
            StorageClient = storageClient ?? throw new ArgumentNullException(nameof(storageClient));
            FFmpegProcess = ffmpegProcess ?? throw new ArgumentNullException(nameof(ffmpegProcess));
            HttpClient = httpClientFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        private IStorageClient StorageClient { get; }

        private IFFmpegProcess FFmpegProcess { get; }

        private HttpClient HttpClient { get; }

        public string Name => nameof(ExtractThumbnail);

        private async Task<Stream> GetHttpStreamAsync(Uri url)
        {
            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        public async Task ExecuteAsync(ProcessJobAssignmentHelper<TransformJob> jobAssignmentHelper, McmaWorkerRequestContext requestContext)
        {
            var logger = jobAssignmentHelper.RequestContext.Logger;
            var options = jobAssignmentHelper.Profile.CustomProperties?.ToMcmaObject<ExtractThumbnailOptions>() ?? new ExtractThumbnailOptions();
            
            Locator inputFile;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            var tempId = Guid.NewGuid().ToString();
            var tempVideoFile = "/tmp/video_" + tempId + ".mp4";
            var tempThumbFile = "/tmp/thumb_" + tempId + ".png";

            try
            {
                logger.Info("Get video from location: " + inputFile.Url);

                if (!Uri.TryCreate(inputFile.Url, UriKind.Absolute, out var inputUrl))
                    throw new McmaException($"Unable to parse input url '{inputFile.Url}'");

                var source = inputUrl.Scheme.ToLower() switch
                {
                    "http" or "https"  => await GetHttpStreamAsync(inputUrl),
                    "file" => File.OpenRead(inputUrl.LocalPath),
                    var x => throw new McmaException($"FFmpeg service does not currently support the {x} scheme for input files")
                };

                using (var tempVideoFileStream = File.Create(tempVideoFile))
                using (source)
                    await source.CopyToAsync(tempVideoFileStream);
            
                await FFmpegProcess.RunAsync(
                    "-i",
                    tempVideoFile,
                    "-ss",
                    "00:00:00.500",
                    "-vframes",
                    "1",
                    "-vf",
                    "scale=200:-1",
                    tempThumbFile);

                var outputUrl = $"{options.OutputLocation.TrimEnd('/')}/{jobAssignmentHelper.Job.Id}/{tempId}.png";

                using (var tempThumbFileStream = File.OpenRead(tempThumbFile))
                    await StorageClient.UploadAsync(outputUrl, tempThumbFileStream);

                var presignedOutputUrl = await StorageClient.GetPresignedUrlAsync(outputUrl, PresignedUrlAccessType.Read);
                
                jobAssignmentHelper.JobOutput.Set(nameof(outputUrl), presignedOutputUrl);

                await jobAssignmentHelper.CompleteAsync();
            }
            finally
            {
                try
                {
                    if (File.Exists(tempVideoFile))
                        File.Delete(tempVideoFile);
                }
                catch
                {
                    // just ignore this
                }

                try
                {
                    if (File.Exists(tempThumbFile))
                        File.Delete(tempThumbFile);
                }
                catch
                {
                    // just ignore this
                }
            }
        }
    }
}