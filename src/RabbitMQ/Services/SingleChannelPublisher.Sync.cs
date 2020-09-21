namespace RabbitMQ.Services
{
    using RabbitMQ;
    using RabbitMQ.Entities;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class SingleChannelPublisher<TRequest, TResponse> : IPublisher<TRequest, TResponse>
    {
        private readonly Connection _connection;
        private Channel<TRequest, TResponse>? _model;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private IDictionary<string, Channel<TRequest, TResponse>> _channels = new Dictionary<string, Channel<TRequest, TResponse>>();

        public SingleChannelPublisher(Connection connection)
        {
            _connection = connection;
        }

        private Channel<TRequest, TResponse> Channel => _model ??= _connection.CreateChannel<TRequest, TResponse>(null);

        public async Task<TResponse> Publish(TRequest request, CancellationToken cancellationToken)
        {
            if (_connection.IsClosed)
                _connection.TryConnect();
            try
            {
                await _semaphoreSlim.WaitAsync();
                if (Channel.IsClosed)
                    RemoveChannel();

                return await Channel.Publish(request, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<TResponse> Publish(TRequest request, CancellationToken ctx, string queue_name)
        {
            if (_connection.IsClosed)
                _connection.TryConnect();

            try
            {
                await _semaphoreSlim.WaitAsync();
                var channel = GetOrAdd(queue_name);
                return await channel.Publish(request, ctx);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Obtem um channel novo
        /// </summary>
        /// <param name="key">nome da fila</param>
        /// <returns>Channel</returns>
        private Channel<TRequest, TResponse> GetOrAdd(string key)
        {
            var containsChannel = _channels.ContainsKey(key);
            Channel<TRequest, TResponse> channel = containsChannel ? _channels[key] : _connection.CreateChannel<TRequest, TResponse>(key);


            if (containsChannel && channel.IsClosed)
            {
                channel.Dispose();
                _channels.Remove(key);
                channel = _connection.CreateChannel<TRequest, TResponse>(key);
                containsChannel = false;
            }

            if (!containsChannel)
                _channels.Add(key, channel);

            return channel;
        }

        private void RemoveChannel()
        {
            _model?.Dispose();
            _model = null;
        }

        public void Dispose()
        {
            _semaphoreSlim.Dispose();
            _model?.Dispose();
            foreach (var channel in _channels.Values)
                channel.Dispose();

            _channels.Clear();
            _connection.Dispose();
        }
    }
}