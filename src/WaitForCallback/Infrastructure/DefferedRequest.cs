namespace WaitForCallback.Infrastructure
{
    public class DefferedRequest : IDisposable
    {
        public RequestPayload? Payload { get; set; }

        public TaskCompletionSource<RequestPayload>? TaskCompletionSource { get; set; }

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
