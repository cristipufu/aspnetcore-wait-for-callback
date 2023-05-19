namespace WaitForCallback.Infrastructure
{
    public class DefferedRequest<T> : IDisposable
    {
        public RequestPayload<T>? Payload { get; set; }

        public TaskCompletionSource<RequestPayload<T>>? TaskCompletionSource { get; set; }

        public CancellationTokenSource? TimeoutCancellationTokenSource { get; set; }

        public CancellationTokenSource? CancellationTokenSource { get; set; }

        public CancellationTokenRegistration CancellationTokenRegistration { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                TimeoutCancellationTokenSource?.Dispose();
                CancellationTokenSource?.Dispose();
                CancellationTokenRegistration.Dispose();
            }
        }
    }
}
