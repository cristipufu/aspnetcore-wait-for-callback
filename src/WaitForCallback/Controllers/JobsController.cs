using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WaitForCallback.Infrastructure;

namespace WaitForCallback.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly IRequestsQueue _requestsQueue;

        public JobsController(IRequestsQueue requestsQueue)
        {
            _requestsQueue = requestsQueue;
        }

        [HttpPost("Start")]
        public async Task<ActionResult> Start()
        {
            var newJobId = Guid.NewGuid();

            // Do deferred work in the background.
            // We assume once the task completes, the worker will call the [POST]Jobs/Complete API.
            _ = RunAsync(newJobId);

            var pendingJob = new JobModel
            {
                Id = newJobId,
                Status = "pending",
                Timestamp = DateTime.UtcNow,
            };

            var request = await _requestsQueue.EnqueueRequestAsync(new RequestPayload
            {
                Key = newJobId,
            }, HttpContext.RequestAborted);

            var completedJob = request.GetPayload<JobModel>();

            if (completedJob != null)
            {
                pendingJob.CompletedTimestamp = completedJob.CompletedTimestamp;
                pendingJob.Duration = completedJob.CompletedTimestamp - pendingJob.Timestamp;
                pendingJob.Status = completedJob.Status;
            }

            return Ok(pendingJob);
        }

        [HttpPost("Complete/{jobId}")]
        public async Task<ActionResult> Complete(Guid jobId)
        {
            var request = new RequestPayload
            {
                Key = jobId,
            };

            request.SetPayload(new JobModel
            {
                Id = jobId,
                Status = "completed",
                CompletedTimestamp = DateTime.UtcNow,
            });

            await _requestsQueue.DequeueRequestAsync(request);

            return Ok();
        }

        private static async Task RunAsync(Guid jobId)
        {
            await Task.Delay(5000);

            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://localhost:7038/Jobs/Complete/{jobId}");

            await httpClient.SendAsync(request);
        }
    }
}