using Microsoft.AspNetCore.Mvc;
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

            var requestPayload = await _requestsQueue.EnqueueRequestAsync(new RequestPayload<JobModel>
            {
                Key = newJobId,
                Data = new JobModel
                {
                    Id = newJobId,
                    Status = "completed",
                    Timestamp = DateTime.UtcNow,
                },
            }, HttpContext.RequestAborted);

            return Ok(((RequestPayload<JobModel>)requestPayload).Data);
        }

        [HttpPost("Complete/{jobId}")]
        public async Task<ActionResult> Complete(Guid jobId)
        {
            await _requestsQueue.DequeueRequestAsync(jobId);

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