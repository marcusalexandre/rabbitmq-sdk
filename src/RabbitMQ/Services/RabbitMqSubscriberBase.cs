namespace RabbitMQ.Services
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using RabbitMQ.Entities;
    using RabbitMQ.ValueObjects;
    using System;
    using System.Threading;

    /// <summary>
    /// Base para subscribers, trabalha como um serviço em segundo plano no .net,
    /// observando o ciclo de vida definido pelo aplicativo executor
    /// </summary>
    public abstract class RabbitMqSubscriberBase : BackgroundService
    {
        protected Connection _connection;
        private Channel _channel;
        protected readonly ILogger<RabbitMqSubscriberBase> _logger;

        /// <summary>
        /// Hook para execução de eventos
        /// </summary>
        public event EventHandler BeforeSubscribe;

        /// <summary>
        /// Base para subscribers, trabalha como um serviço em segundo plano no .net,
        /// observando o ciclo de vida definido pelo aplicativo executor
        /// </summary>
        /// <param name="connection">Conexao </param>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public RabbitMqSubscriberBase(Connection connection, ILogger<RabbitMqSubscriberBase> logger)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger;
        }

        /// <summary>
        /// Aciona o hook do evento de interceptação de criação de canal, se assinado
        /// </summary>
        /// <param name="channel">Canal</param>
        protected void OnBeforeSubscribe(Channel channel)
        {
            BeforeSubscribe?.Invoke(this, new BeforeSubscribeEventArgs(channel.Descriptor));
        }

        /// <summary>
        /// Tentativa de conexão com o RabbitMq
        /// </summary>
        /// <param name="stoppingToken">Token de cancelamento fornecido pelo entrypoint da aplicação</param>
        protected void TryConnect(CancellationToken stoppingToken)
        {
            try
            {
                if (_connection?.IsClosed ?? true)
                {
                    _channel?.Dispose();
                    _connection?.TryConnect();
                    _channel = CreateAndSubscribe(stoppingToken);
                }

                if (_channel?.IsClosed ?? true)
                {
                    _channel?.Dispose();
                    _channel = CreateAndSubscribe(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("RabbitMq must be offline.", ex);
            }
        }


        /// <summary>
        /// Cria e subscreve um canal
        /// </summary>
        /// <param name="stoppingToken">Token de cancelamento</param>
        /// <returns>Canal</returns>
        protected abstract Channel CreateAndSubscribe(CancellationToken stoppingToken);
    }
}