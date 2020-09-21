namespace RabbitMQ.Services
{
    using Microsoft.Extensions.Logging;
    using RabbitMQ.Entities;
    using System;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscrição de eventos síncronos com o RabbitMq 
    /// </summary>
    /// <typeparam name="TRequest">Tipo da Requisição</typeparam>
    public class RabbitMqSubscriber<TRequest, TResponse> : RabbitMqSubscriberBase
    {
        /// <summary>
        /// Expressão de para chamada de handler
        /// </summary>
        public static Expression<Func<object, Func<TRequest, TResponse>>>? Handler { get; internal set; }

        /// <summary>
        /// Publicação de eventos sincronos com o RabbitMq 
        /// </summary>
        /// <param name="connectionFactory">Connection Factory</param>
        /// <param name="services">netcore Ioc</param>
        /// <param name="config">Arquivos de Configuração</param>
        /// <param name="logger">netcore Logger</param>
        public RabbitMqSubscriber(Connection connection,
            ILogger<RabbitMqSubscriber<TRequest, TResponse>> logger) : base(connection, logger)
        {
        }

        /// <summary>
        /// Consumidor de Mensagens
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TryConnect(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    TryConnect(stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                catch (TaskCanceledException ex)
                {
                    _logger?.LogInformation("Subscriber canceled by requestor", ex);
                }
            }
        }

        /// <summary>
        /// Cria e subscreve o canal
        /// </summary>
        /// <param name="stoppingToken">Token de cancelamento</param>
        /// <returns>Canal</returns>
        protected override Channel CreateAndSubscribe(CancellationToken stoppingToken)
        {
            var channel = _connection.CreateChannel<TRequest, TResponse>(null);
            OnBeforeSubscribe(channel);
            channel.Subscribe(stoppingToken);
            return channel;
        }
    }
}