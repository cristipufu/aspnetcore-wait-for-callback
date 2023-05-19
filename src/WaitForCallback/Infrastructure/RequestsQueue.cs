using System.Collections.Concurrent;

namespace WaitForCallback.Infrastructure
{
    public class RequestsQueue : IRequestsQueue
    {
        public ConcurrentDictionary<Guid, DefferedRequest> PendingRequests = new();

        public RequestsQueue()
        {

        }

        public virtual Task<RequestPayload> EnqueueRequestAsync(RequestPayload payload, CancellationToken cancellationToken)
        {
            DefferedRequest request = new()
            {
                Payload = payload,
                TimeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)), // wait max 10 seconds
                TaskCompletionSource = new TaskCompletionSource<RequestPayload>(),
            };

            // Wait until caller cancels or timeout expires
            request.CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    request.TimeoutCancellationTokenSource.Token);

            PendingRequests.TryAdd(request.Payload.Key, request);

            if (request.CancellationTokenSource.Token.CanBeCanceled)
            {
                request.CancellationTokenRegistration = request.CancellationTokenSource.Token.Register(obj =>
                {
                    // When the request gets canceled
                    var request = (DefferedRequest)obj!;
                    request.TaskCompletionSource!.TrySetCanceled(request.CancellationTokenSource!.Token);

                    PendingRequests.TryRemove(request.Payload!.Key, out var _);

                    request.Dispose();

                }, request);
            }

            return request.TaskCompletionSource.Task;
        }

        public virtual Task DequeueRequestAsync(RequestPayload payload)
        {
            if (!PendingRequests.TryRemove(payload.Key, out var request))
            {
                return Task.CompletedTask;
            }

            if (!request.TaskCompletionSource!.Task.IsCompleted)
            {
                // Try to complete the task 
                if (request.TaskCompletionSource?.TrySetResult(payload) == false)
                {
                    // The request was canceled
                }
            }
            else
            {
                // The request was canceled while pending
            }

            request.Dispose();

            return Task.CompletedTask;
        }
    }
}
