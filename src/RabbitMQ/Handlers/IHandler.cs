namespace RabbitMQ.Handlers
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Contrato de Handler para utilização sincrona
    /// </summary>
    /// <typeparam name="TRequest">Modelo de Requisição</typeparam>
    public interface IHandler<TRequest>
    {
        /// <summary>
        /// Handle de Rececimento da mensagem
        /// </summary>
        /// <param name="request">Requisição</param>
        /// <param name="ctx">Token de cancelamento</param>
        Task Handle(TRequest request, CancellationToken ctx);
    }
}
