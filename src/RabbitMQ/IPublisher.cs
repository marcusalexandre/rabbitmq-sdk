namespace RabbitMQ
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publicação de eventos na fila específicada
    /// </summary>
    /// <typeparam name="TRequest">Modelo de Domínio</typeparam>
    public interface IPublisher<TRequest> : IDisposable
    {
        /// <summary>
        /// Publica na fila o modelo
        /// </summary>
        /// <param name="request">Modelo</param>
        /// <param name="ctx">Token de cancelamento para operações asíncronas</param>
        /// <param name="queue_name">Nome da fila, se motido, gera conforme especifícação de modelo</param>
        Task Publish(TRequest request, CancellationToken ctx, string queue_name, TimeSpan? enqueueTime = null);

        /// <summary>
        /// Publica na fila o modelo
        /// </summary>
        /// <param name="request">Modelo</param>
        /// <param name="ctx">Token de cancelamento para operações asíncronas</param>
        Task Publish(TRequest request, CancellationToken ctx, TimeSpan? enqueueTime = null);
    }
}
