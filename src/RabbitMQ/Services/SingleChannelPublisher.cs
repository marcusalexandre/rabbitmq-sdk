namespace RabbitMQ.Services
{
    using RabbitMQ;
    using RabbitMQ.Entities;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class SingleChannelPublisher<TRequest> : IPublisher<TRequest>
    {
        private readonly Connection _connection;
        private Channel<TRequest>? _model;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private IDictionary<string, Channel<TRequest>> _channels = new Dictionary<string, Channel<TRequest>>();

        public SingleChannelPublisher(Connection connection)
        {
            _connection = connection;
        }

        private Channel<TRequest> Channel => _model ??= _connection.CreateChannel<TRequest>();
        public async Task Publish(TRequest request, CancellationToken cancellationToken, TimeSpan? enqueueTime = null)
        {

            if (_connection.IsClosed)
                _connection.TryConnect();
            try
            {
                await _semaphoreSlim.WaitAsync();
                if (Channel.IsClosed)
                    RemoveChannel();

                await Channel.Publish(request, cancellationToken, enqueueTime);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task Publish(TRequest request, CancellationToken ctx, string queue_name, TimeSpan? enqueueTime = null)
        {
            if (_connection.IsClosed)
                _connection.TryConnect();

            try
            {
                await _semaphoreSlim.WaitAsync();
                var channel = GetOrAdd(queue_name);
                await channel.Publish(request, ctx, enqueueTime);
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
        private Channel<TRequest> GetOrAdd(string key)
        {
            var containsChannel = _channels.ContainsKey(key);
            Channel<TRequest> channel = containsChannel ? _channels[key] : _connection.CreateChannel<TRequest>(key);


            if (containsChannel && channel.IsClosed)
            {
                channel.Dispose();
                _channels.Remove(key);
                channel = _connection.CreateChannel<TRequest>(key);
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