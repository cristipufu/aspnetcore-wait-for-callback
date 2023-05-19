using System.Collections.Concurrent;

namespace WaitForCallback.Infrastructure
{
    public class RequestsQueue : IRequestsQueue
    {
        public ConcurrentDictionary<Guid, DefferedRequest> PendingRequests = new();

        public RequestsQueue() { }

        public virtual async Task<RequestPayload<T>> EnqueueRequestAsync<T>(RequestPayload<T> payload, TimeSpan waitForTimeout, CancellationToken cancellationToken)
        {
            DefferedRequest request = new()
            {
                Payload = payload,
                TimeoutCancellationTokenSource = new CancellationTokenSource(waitForTimeout), // wait max 10 seconds
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

                    if (request.TimeoutCancellationTokenSource!.IsCancellationRequested)
                    {
                        request.TaskCompletionSource!.TrySetResult(request.Payload!);
                    }
                    else
                    {
                        // Canceled by caller
                        request.TaskCompletionSource!.TrySetCanceled(request.CancellationTokenSource!.Token);
                    }

                    PendingRequests.TryRemove(request.Payload!.Key, out var _);

                    request.Dispose();

                }, request);
            }

            var response = await request.TaskCompletionSource.Task;

            return (RequestPayload<T>)response;
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
