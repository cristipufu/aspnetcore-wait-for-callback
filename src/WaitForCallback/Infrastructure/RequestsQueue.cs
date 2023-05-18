namespace WaitForCallback.Infrastructure
{
    public class RequestsQueue : IRequestsQueue
    {
        public Dictionary<Guid, DefferedRequest> PendingRequests = new();

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

            PendingRequests.Add(request.Payload.Key, request);

            if (request.CancellationTokenSource.Token.CanBeCanceled)
            {
                request.CancellationTokenRegistration = request.CancellationTokenSource.Token.Register(obj =>
                {
                    // When the request gets canceled
                    var request = (DefferedRequest)obj!;
                    request.TaskCompletionSource!.TrySetCanceled(request.CancellationTokenSource!.Token);

                    PendingRequests.Remove(request.Payload!.Key);

                    request.Dispose();

                }, request);
            }

            return request.TaskCompletionSource.Task;
        }

        public virtual Task DequeueRequestAsync(Guid key)
        {
            if (!PendingRequests.TryGetValue(key, out var request))
            {
                return Task.CompletedTask;
            }

            if (!request.TaskCompletionSource!.Task.IsCompleted)
            {
                // Try to complete the task 
                if (request.TaskCompletionSource?.TrySetResult(request.Payload!) == false)
                {
                    // The request was canceled
                }
            }
            else
            {
                // The request was canceled while pending
            }

            request.Dispose();

            PendingRequests.Remove(key);

            return Task.CompletedTask;
        }
    }
}
