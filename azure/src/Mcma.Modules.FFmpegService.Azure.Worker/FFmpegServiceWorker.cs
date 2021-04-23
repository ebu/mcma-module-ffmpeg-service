using System;
using System.Threading.Tasks;
using Mcma.Worker;
using Mcma.Worker.Common;
using Microsoft.Azure.WebJobs;

namespace Mcma.Modules.FFmpegService.Azure.Worker
{
    public class FFmpegServiceWorker
    {
        public FFmpegServiceWorker(IMcmaWorker worker)
        {
            Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        }

        private IMcmaWorker Worker { get; }
            
        [FunctionName(nameof(FFmpegServiceWorker))]
        public async Task ExecuteAsync(
            [QueueTrigger("ffmpeg-service-work-queue", Connection = "MCMA_WORK_QUEUE_STORAGE")] McmaWorkerRequest workerRequest,
            ExecutionContext executionContext)
        {
            await Worker.DoWorkAsync(workerRequest, executionContext.InvocationId.ToString());
        }
    }
}