using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Model;
using Mcma.Model.Jobs;
using Mcma.Storage;
using Mcma.Worker;
using Mcma.Worker.Jobs;
using Microsoft.Extensions.Options;

namespace Mcma.Modules.FFmpegService.Worker
{
    public class ExtractThumbnail : IJobProfile<TransformJob>
    {
        public ExtractThumbnail(IStorageClient storageClient, IFFmpegProcess ffmpegProcess, IHttpClientFactory httpClientFactory, IOptions<ExtractThumbnailOptions> options)
        {
            StorageClient = storageClient ?? throw new ArgumentNullException(nameof(storageClient));
            FFmpegProcess = ffmpegProcess ?? throw new ArgumentNullException(nameof(ffmpegProcess));
            HttpClient = httpClientFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
            Options = options.Value ?? new ExtractThumbnailOptions();
        }

        private IStorageClient StorageClient { get; }

        private IFFmpegProcess FFmpegProcess { get; }

        private HttpClient HttpClient { get; }
        
        private ExtractThumbnailOptions Options { get; }

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
            
            Locator inputFile;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            var tempId = Guid.NewGuid().ToString();
            var tempFolder = Path.Combine(Options.TempOutputFolder, tempId);
            var tempVideoFile = Path.Combine(tempFolder, Path.GetFileName(inputFile.Url));
            var outputFileName = Path.GetFileNameWithoutExtension(inputFile.Url) + ".png";
            var tempThumbFile = Path.Combine(tempFolder, outputFileName);

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

                Directory.CreateDirectory(tempFolder);

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

                var outputUrl = $"{Options.OutputLocation.TrimEnd('/')}/{tempId}/{outputFileName}";

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