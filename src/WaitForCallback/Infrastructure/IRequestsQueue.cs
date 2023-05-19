namespace WaitForCallback.Infrastructure
{
    public interface IRequestsQueue
    {
        Task<RequestPayload<T>> EnqueueRequestAsync<T>(RequestPayload<T> payload, TimeSpan waitForTimeout, CancellationToken cancellationToken);

        Task DequeueRequestAsync(RequestPayload payload);
    }
}
