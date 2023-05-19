using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WaitForCallback.Infrastructure;
using WaitForCallback.Models;

namespace WaitForCallback.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly IRequestsQueue<JobModel> _requestsQueue;

        public JobsController(IRequestsQueue<JobModel> requestsQueue)
        {
            _requestsQueue = requestsQueue;
        }

        [HttpPost("Start")]
        public async Task<ActionResult> Start([FromBody]StartJobModel settings)
        {
            var pendingJob = new JobModel
            {
                Id = Guid.NewGuid(),
                Status = "pending",
                Timestamp = DateTime.UtcNow,
            };

            // Do deferred work in the background.
            // We assume once the task completes, the worker will call the [POST]Jobs/Complete API.
            _ = RunAsync(pendingJob);

            if (!settings.WaitForResponse)
            {
                return Ok(pendingJob);
            }

            var request = await _requestsQueue.EnqueueRequestAsync(new RequestPayload<JobModel>
            {
                Key = pendingJob.Id,
                Payload = pendingJob,
            }, TimeSpan.FromSeconds(settings.WaitForTimeoutSeconds), HttpContext.RequestAborted);

            return Ok(request.Payload);
        }

        [HttpPost("Complete")]
        public async Task<ActionResult> Complete([FromBody]JobModel job)
        {
            job.Status = "completed";
            job.CompletedTimestamp = DateTime.UtcNow;
            job.Duration = job.CompletedTimestamp - job.Timestamp;

            var request = new RequestPayload<JobModel>
            {
                Key = job.Id,
                Payload = job,
            };

            await _requestsQueue.DequeueRequestAsync(request);

            return Ok();
        }

        private static async Task RunAsync(JobModel job)
        {
            await Task.Delay(5000);

            var httpClient = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7038/Jobs/Complete")
            {
                Content = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json")
            };

            await httpClient.SendAsync(request);
        }
    }
}