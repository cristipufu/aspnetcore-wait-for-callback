using Newtonsoft.Json;
using StackExchange.Redis;

namespace WaitForCallback.Infrastructure
{
    public class RedisRequestsQueue<T> : IRequestsQueue<T>
    {
        private const string Channel = "deffered-requests-channel";

        private readonly IConnectionMultiplexer _connectionMultiplexer;

        private readonly RequestsQueue<T> _requestsQueue;

        public RedisRequestsQueue(IConnectionMultiplexer connectionMultiplexer)
        {
            _requestsQueue = new RequestsQueue<T>();

            _connectionMultiplexer = connectionMultiplexer;

            var pubSub = _connectionMultiplexer.GetSubscriber();

            pubSub.Subscribe(Channel).OnMessage(async (channelMsg) =>
            {
                var request = JsonConvert.DeserializeObject<RequestPayload<T>>(channelMsg.Message!);

                await _requestsQueue.DequeueRequestAsync(request!);
            });
        }

        public Task<RequestPayload<T>> EnqueueRequestAsync(RequestPayload<T> payload, TimeSpan waitForTimeout, CancellationToken cancellationToken)
        {
            return _requestsQueue.EnqueueRequestAsync(payload, waitForTimeout, cancellationToken);
        }

        public async Task DequeueRequestAsync(RequestPayload<T> payload)
        {
            // Start request was made on the same node.
            if (_requestsQueue.PendingRequests.ContainsKey(payload.Key))
            {
                await _requestsQueue.DequeueRequestAsync(payload);

                return;
            }

            // Start request was initially made on another node.
            // Broadcast completion to other nodes.
            var pubSub = _connectionMultiplexer.GetSubscriber();

            await pubSub.PublishAsync(Channel, new RedisValue(JsonConvert.SerializeObject(payload)));
        }
    }
}
