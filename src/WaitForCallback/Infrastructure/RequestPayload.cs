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

    public class RequestPayload<T> : RequestPayload
    {
        public T? Payload
        {
            get
            {
                return GetPayload<T>();
            }
            set
            {
                if (value != null)
                {
                    SetPayload(value);
                }
            }
        }
    }
}
