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
                TaskId = newTaskId,
                TimeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)), // wait max 10 seconds
                TaskCompletionSource = new TaskCompletionSource<Guid>(),
            };

            // Wait until caller cancels or timeout expires
            request.CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    HttpContext.RequestAborted,
                    request.TimeoutCancellationTokenSource.Token);

            if (request.CancellationTokenSource.Token.CanBeCanceled)
            {
                request.CancellationTokenRegistration = request.CancellationTokenSource.Token.Register(static obj =>
                {
                    // When the request gets canceled
                    var request = (TaskRequest)obj!;
                    request.TaskCompletionSource!.TrySetCanceled(request.CancellationTokenSource!.Token);

                    PendingTasks.Remove(request.TaskId);

                    request.Dispose();

                }, request);
            }

            PendingTasks.Add(newTaskId, request);

            // Do deferred work in the background.
            // We assume once the task completes, the worker will call the [POST]Task/Complete API.
            _ = RunAsync(newTaskId);
            
            return Ok(await request.TaskCompletionSource.Task);
        }

        [HttpPost("Complete/{taskId}")]
        public ActionResult Complete(Guid taskId)
        {
            if (!PendingTasks.TryGetValue(taskId, out var request))
            {
                return Ok();
            }

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

            request.Dispose();

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

        private record TaskRequest : IDisposable
        {
            public Guid TaskId { get; set; }

            public CancellationTokenSource? TimeoutCancellationTokenSource { get; set; }

            public CancellationTokenSource? CancellationTokenSource { get; set; }

            public TaskCompletionSource<Guid>? TaskCompletionSource { get; set; }

            public CancellationTokenRegistration CancellationTokenRegistration { get; set; }

            public void Dispose()
            {
                TimeoutCancellationTokenSource?.Dispose();
                CancellationTokenSource?.Dispose();
                CancellationTokenRegistration.Dispose();
            }
        }
    }
}