using Newtonsoft.Json;
using StackExchange.Redis;

namespace WaitForCallback.Infrastructure
{
    public class RedisRequestsQueue : IRequestsQueue
    {
        private const string Channel = "deffered-requests-channel";

        private readonly IConnectionMultiplexer _connectionMultiplexer;

        private readonly RequestsQueue _requestsQueue;

        public RedisRequestsQueue(IConnectionMultiplexer connectionMultiplexer)
        {
            _requestsQueue = new RequestsQueue();

            _connectionMultiplexer = connectionMultiplexer;

            var pubSub = _connectionMultiplexer.GetSubscriber();

            pubSub.Subscribe(Channel).OnMessage(async (channelMsg) =>
            {
                var request = JsonConvert.DeserializeObject<RequestPayload>(channelMsg.Message!);

                await _requestsQueue.DequeueRequestAsync(request!);
            });
        }

        public Task<RequestPayload<T>> EnqueueRequestAsync<T>(RequestPayload<T> payload, TimeSpan waitForTimeout, CancellationToken cancellationToken)
        {
            return _requestsQueue.EnqueueRequestAsync(payload, waitForTimeout, cancellationToken);
        }

        public async Task DequeueRequestAsync(RequestPayload payload)
        {
            if (_requestsQueue.PendingRequests.ContainsKey(payload.Key))
            {
                await _requestsQueue.DequeueRequestAsync(payload);

                return;
            }

            var pubSub = _connectionMultiplexer.GetSubscriber();

            await pubSub.PublishAsync(Channel, new RedisValue(JsonConvert.SerializeObject(payload)));
        }
    }
}
