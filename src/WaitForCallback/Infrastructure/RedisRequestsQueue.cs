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
                var key = Guid.Parse(channelMsg.Message!);

                await _requestsQueue.DequeueRequestAsync(key);
            });
        }

        public Task<RequestPayload> EnqueueRequestAsync(RequestPayload payload, CancellationToken cancellationToken)
        {
            return _requestsQueue.EnqueueRequestAsync(payload, cancellationToken);
        }

        public async Task DequeueRequestAsync(Guid key)
        {
            if (_requestsQueue.PendingRequests.ContainsKey(key))
            {
                await _requestsQueue.DequeueRequestAsync(key);

                return;
            }

            var pubSub = _connectionMultiplexer.GetSubscriber();

            await pubSub.PublishAsync(Channel, new RedisValue(key.ToString()));
        }
    }
}
