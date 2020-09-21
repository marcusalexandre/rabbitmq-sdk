namespace RabbitMQ.Handlers
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Contrato de Handler RPC para tratamento de filas
    /// </summary>
    /// <typeparam name="TRequest">Modelo de Requisição</typeparam>
    /// <typeparam name="TResponse">Modelo de Resposta</typeparam>
    public interface IHandler<TRequest, TResponse>
    {
        /// <summary>
        /// Handler RPC
        /// </summary>
        /// <param name="request">Requisição</param>
        /// <param name="ctx">Token de cancelamento</param>
        /// <returns>Resposta</returns>
        Task<TResponse> Handle(TRequest request, CancellationToken ctx);
    }
}
