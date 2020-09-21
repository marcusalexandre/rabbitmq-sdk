namespace RabbitMQ.Services
{
    using Microsoft.Extensions.Logging;
    using RabbitMQ.Entities;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscrição de eventos asincronos com o RabbitMq 
    /// </summary>
    /// <typeparam name="TRequest">Tipo da Requisição</typeparam>
    public class RabbitMqSubscriber<TRequest> : RabbitMqSubscriberBase
    {
        /// <summary>
        /// Subscrição de eventos asincronos com o RabbitMq 
        /// </summary>
        /// <param name="connectionFactory">Connection Factory</param>
        /// <param name="services">netcore Ioc</param>
        /// <param name="config">Arquivos de Configuração</param>
        /// <param name="logger">netcore Logger</param>
        public RabbitMqSubscriber(Connection connection,
                                  ILogger<RabbitMqSubscriber<TRequest>> logger)
            : base(connection, logger)
        {
        }


        /// <summary>
        /// Consumidor de Mensagens
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    TryConnect(stoppingToken);

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogInformation("Subscriber canceled by requestor", ex);
                }
            }
        }

        /// <summary>
        /// Cria e subscreve o canal
        /// </summary>
        /// <param name="stoppingToken">Token de Cancelamento</param>
        /// <returns>Canal</returns>
        protected override Channel CreateAndSubscribe(CancellationToken stoppingToken)
        {
            var channel = _connection.CreateChannel<TRequest>();
            OnBeforeSubscribe(channel);
            channel.Subscribe(stoppingToken);
            return channel;
        }
    }
}