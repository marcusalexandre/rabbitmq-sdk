namespace RabbitMQ
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publicação de eventos na fila específicada, no formato RPC
    /// O agente que receber a mensagem irá devolver a resposta em uma fila RPC do canal,
    ///     este serviço devolverá a resposta pela Task
    ///     
    /// <see cref=">Verifique o parâmetro 'responseTimeoutInSeconds', ele é responsável pelo Timeout deste processo."/>
    /// </summary>
    public interface IPublisher<TRequest, TResponse> : IDisposable
    {
        Task<TResponse> Publish(TRequest request, CancellationToken ctx);
        Task<TResponse> Publish(TRequest request, CancellationToken ctx, string queue_name);
    }
}
