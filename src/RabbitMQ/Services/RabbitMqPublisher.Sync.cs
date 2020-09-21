namespace RabbitMQ.Services
{
    using RabbitMQ.Entities;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publicação de eventos asincronos do tipo RPC com o RabbitMq 
    /// </summary>
    /// <typeparam name="TRequest">Tipo da Requisição</typeparam>
    public class RabbitMqPublisher<TRequest, TResponse> : IPublisher<TRequest, TResponse>
    {
        private readonly Connection _connection;

        /// <summary>
        /// Publicação de eventos asincronos do tipo RPC com o RabbitMq 
        /// </summary>
        /// <param name="services">netcore Ioc</param>
        /// <param name="connectionFactory">Connection Factory</param>
        /// <param name="config">Arquivos de Configuração</param>
        public RabbitMqPublisher(Connection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Publica o evento e aguarda a resposta na fila exclusiva
        /// Segura a requisição ate a resposta ou timeout
        /// </summary>
        /// <param name="request">Requisição</param>
        /// <param name="ctx">Token de Cancelamento</param>
        /// <param name="queueName">Nome da Fila</param>
        /// <returns>Modelo de resposta</returns>
        public async Task<TResponse> Publish(TRequest request, CancellationToken ctx, string? queueName = null)
        {
            if (_connection.IsClosed)
                _connection.TryConnect();

            using var channel = _connection.CreateChannel<TRequest, TResponse>(queueName);
            return await channel.Publish(request, ctx);
        }

        public async Task<TResponse> Publish(TRequest request, CancellationToken ctx)
        {
            if (_connection.IsClosed)
                _connection.TryConnect();

            using var channel = _connection.CreateChannel<TRequest, TResponse>(null);
            return await channel.Publish(request, ctx);
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