namespace WaitForCallback.Infrastructure
{
    public interface IRequestsQueue<T>
    {
        Task<RequestPayload<T>> EnqueueRequestAsync(RequestPayload<T> payload, TimeSpan waitForTimeout, CancellationToken cancellationToken);

        Task DequeueRequestAsync(RequestPayload<T> payload);
    }
}
