namespace WaitForCallback.Infrastructure
{
    public class RequestPayload
    {
        public Guid Key { get; set; }
    }

    public class RequestPayload<T> : RequestPayload
    {
        public T? Data { get; set; }
    }
}
