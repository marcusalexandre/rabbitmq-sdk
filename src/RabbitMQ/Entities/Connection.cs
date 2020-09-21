namespace RabbitMQ.Entities
{
    using Microsoft.Extensions.Logging;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Configuration;
    using RabbitMQ.Serializers;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// Fachada de comunicação com o RabbitMQ
    /// </summary>
    public class Connection : IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly IConnectionFactory _connectionFactory;
        private readonly Serializer _serializer;
        private readonly RabbitMqConfiguration _configuration;
        private readonly ILogger<Connection> _logger;
        object sync_root = new object();
        /// <summary>
        /// Fabrica para Conexões
        /// </summary>
        private IConnection _rabbitConnection;
        private bool _disposed;

        public Connection(IServiceProvider services, IConnectionFactory connectionFactory,
            Serializer serializer, RabbitMqConfiguration configuration)
        {
            _services = services;
            _connectionFactory = connectionFactory;
            _serializer = serializer;
            _configuration = configuration;
            _logger = (ILogger<Connection>)services.GetService(typeof(ILogger<Connection>));
        }


        public bool IsConnected => _rabbitConnection != null && _rabbitConnection.IsOpen && !_disposed;

        public bool IsClosed => !IsConnected;

        /// <summary>
        /// Cria um canal de conexão com uma fila asincrona
        /// </summary>
        public Channel<TRequest> CreateChannel<TRequest>(string? queueName = null)
        {
            if (IsClosed)
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");

            var model = _rabbitConnection.CreateModel();
            model.BasicQos(0, _configuration.PrefetchCount, false);
            var atrr = _configuration.GetMaping<TRequest>();
            var channel = new Channel<TRequest>(_services, model, _serializer, _configuration, atrr, queueName);
            var args = new Dictionary<string, object>();
            if (_configuration.MessagesMaxLength.HasValue)
            {
                args.Add("x-max-length", _configuration.MessagesMaxLength.Value);
            }
            if (_configuration.MessagesBytesMaxLength.HasValue)
            {
                args.Add("max-length-bytes", _configuration.MessagesBytesMaxLength.Value);
            }
            if (_configuration.Ttl.HasValue)
            {
                args.Add("x-message-ttl", (int)_configuration.Ttl.Value.TotalMilliseconds);
            }
            model.QueueDeclare(channel.Descriptor.QueueName, true, false, false, args);
            model.QueueDeclare(channel.Descriptor.DeadLetterQueueName, true, false, true, new Dictionary<string, object>());
            return channel;
        }

        /// <summary>
        /// Cria um canal de conexão com uma fila sincrona
        /// </summary>
        public Channel<TRequest, TResponse> CreateChannel<TRequest, TResponse>(string? queueName)
        {
            return CreateChannel<TRequest, TResponse>(queueName, new BlockingCollection<BasicDeliverEventArgs>());
        }

        /// <summary>
        /// Cria um canal de conexão com uma fila sincrona
        /// </summary>
        public Channel<TRequest, TResponse> CreateChannel<TRequest, TResponse>(string? queueName, BlockingCollection<BasicDeliverEventArgs> blockingCollection)
        {
            if (IsClosed)
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");

            var model = _rabbitConnection.CreateModel();
            var atrr = _configuration.GetMaping<TRequest, TResponse>();
            var channel = new Channel<TRequest, TResponse>(_services, model, _serializer, blockingCollection, _configuration, atrr, queueName);
            model.BasicQos(0, _configuration.PrefetchCount, false);
            model.QueueDeclare(channel.Descriptor.QueueName, true, false, false, new Dictionary<string, object>());
            return channel;
        }

        /// <summary>
        /// Tentativa de conectar no Broker do RabbitMq
        /// </summary>
        /// <returns></returns>
        public bool TryConnect()
        {
            lock (sync_root)
            {
                TryCloseConnection();

                _rabbitConnection = _connectionFactory
                .CreateConnection();

                if (IsConnected)
                {
                    _rabbitConnection.ConnectionShutdown += OnConnectionShutdown;
                    _rabbitConnection.CallbackException += OnCallbackException;
                    _rabbitConnection.ConnectionBlocked += OnConnectionBlocked;


                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void TryCloseConnection()
        {
            try
            {
                _rabbitConnection?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Trying to dispose connection.", ex);
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            TryConnect();
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            TryConnect();
        }


        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _rabbitConnection?.Dispose();
            _serializer.Dispose();
        }

    }
}