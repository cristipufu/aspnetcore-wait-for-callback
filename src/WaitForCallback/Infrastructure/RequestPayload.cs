namespace WaitForCallback.Infrastructure
{
    public class RequestPayload<T>
    {
        public Guid Key { get; set; }

        public T? Payload { get; set; }
    }
}
