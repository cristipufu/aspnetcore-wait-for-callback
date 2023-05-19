using Newtonsoft.Json;

namespace WaitForCallback.Infrastructure
{
    public class RequestPayload
    {
        public Guid Key { get; set; }

        public string? Data { get; set; }

        public void SetPayload<T>(T payloadData)
        {
            Data = JsonConvert.SerializeObject(payloadData);
        }

        public T? GetPayload<T>()
        {
            if (string.IsNullOrEmpty(Data))
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(Data);
        }
    }
}
