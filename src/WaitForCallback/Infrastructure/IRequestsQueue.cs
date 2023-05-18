namespace WaitForCallback.Infrastructure
{
    public interface IRequestsQueue
    {
        Task<RequestPayload> EnqueueRequestAsync(RequestPayload payload, CancellationToken cancellationToken);

        Task DequeueRequestAsync(Guid key);
    }
}
