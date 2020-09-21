namespace RabbitMQ.Services
{
    using RabbitMQ.Entities;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publicação de eventos asincronos com o RabbitMq 
    /// </summary>
    /// <typeparam name="TRequest">Tipo da Requisição</typeparam>
    public class RabbitMqPublisher<TRequest> : IPublisher<TRequest>
    {
        private readonly Connection _connection;

        /// <summary>
        /// Publicação de eventos asíncronos com o RabbitMq 
        /// </summary>
        /// <param name="services">netcore Ioc</param>
        /// <param name="connectionFactory">Connection Factory</param>
        /// <param name="config">Arquivos de Configuração</param>
        public RabbitMqPublisher(Connection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Publicação do Evento
        /// </summary>
        /// <param name="request">Requisição</param>
        /// <param name="ctx">Token de Cancelamento</param>
        /// <param name="queue_name">Nome da fila, se omitido usa a nomeclatura automática conforme os parâmetros</param>
        public async Task Publish(TRequest request, CancellationToken ctx, string? queue_name = null, TimeSpan? enqueueTime = null)
        {
            if (_connection.IsClosed)
                _connection.TryConnect();

            using var channel = _connection.CreateChannel<TRequest>(queue_name);
            await channel.Publish(request, ctx, enqueueTime);
        }

        public async Task Publish(TRequest request, CancellationToken ctx, TimeSpan? enqueueTime = null)
        {
            if (_connection.IsClosed)
                _connection.TryConnect();

            using var channel = _connection.CreateChannel<TRequest>();
            await channel.Publish(request, ctx);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _connection.Dispose();
        }

    }
}