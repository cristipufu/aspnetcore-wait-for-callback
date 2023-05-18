using Microsoft.AspNetCore.Mvc;

namespace WaitForCallback.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TaskController : ControllerBase
    {
        private static readonly Dictionary<Guid, TaskRequest> PendingTasks = new();

        public TaskController() { }

        [HttpPost("Create")]
        public async Task<ActionResult> Create()
        {
            var newTaskId = Guid.NewGuid();

            TaskRequest request = new()
            {
                CancellationToken = HttpContext.RequestAborted,
                TaskCompletionSource = new TaskCompletionSource<Guid>(),
            };

            if (request.CancellationToken.CanBeCanceled)
            {
                request.CancellationTokenRegistration = request.CancellationToken.Register(static obj =>
                {
                    // When the request gets canceled
                    var request = (TaskRequest)obj!;
                    request.TaskCompletionSource!.TrySetCanceled(request.CancellationToken);

                }, request);
            }

            PendingTasks.Add(newTaskId, request);

            // Do deferred work in the background.
            // We assume once the task completes, the worker will call the [POST]Task/Complete API.
            await RunAsync(newTaskId);
            
            return Ok(await request.TaskCompletionSource.Task);
        }

        [HttpPost("Complete/{taskId}")]
        public ActionResult Complete(Guid taskId)
        {
            var request = PendingTasks[taskId];

            if (!request.TaskCompletionSource!.Task.IsCompleted) 
            {
                // Try to complete the task 
                if (request.TaskCompletionSource?.TrySetResult(taskId) == false) 
                {
                    // The request was canceled
                }
            }
            else
            {
                // The request was canceled while pending
            }

            request.CancellationTokenRegistration.Dispose();

            PendingTasks.Remove(taskId);

            return Ok();
        }

        private static async Task RunAsync(Guid taskId)
        {
            await Task.Delay(5000);

            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://localhost:7038/Task/Complete/{taskId}");

            await httpClient.SendAsync(request);
        }

        private record TaskRequest
        {
            public CancellationToken CancellationToken { get; set; }

            public TaskCompletionSource<Guid>? TaskCompletionSource { get; set; }

            public CancellationTokenRegistration CancellationTokenRegistration { get; set; }
        }
    }
}