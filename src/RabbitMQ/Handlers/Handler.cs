namespace RabbitMQ.Handlers
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Fachada de implementação de Handler, para definição por atributos
    /// </summary>
    /// <typeparam name="THandler">Tipo do handler</typeparam>
    /// <typeparam name="TRequest">Tipo do modelo de requisição</typeparam>
    public class Handler<THandler, TRequest> : IHandler<TRequest>
    {
        /// <summary>
        /// Provedor de serviços
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Método de consumo do subscriber
        /// </summary>
        public Expression<Func<THandler, Func<TRequest, CancellationToken, Task>>> Handle { get; }

        /// <summary>
        /// Fachada de implementação de Handler, para definição por atributos
        /// </summary>
        /// <param name="services">Provedor de serviço</param>
        /// <param name="handle">Método de consumo</param>
        public Handler(IServiceProvider services, Expression<Func<THandler, Func<TRequest, CancellationToken, Task>>> handle)
        {
            Services = services;
            Handle = handle;
        }

        /// <summary>
        /// Fachada de implementação de Handler, para definição por atributos
        /// </summary>
        /// <param name="services">Provedor de serviço</param>
        /// <param name="methodInfo">Reflection do método de uso</param>
        public Handler(IServiceProvider services, MethodInfo methodInfo)
            : this(services, h => (req, ctx) => (Task)methodInfo.Invoke(h, new object[] { req, ctx }))
        {

        }

        /// <summary>
        /// Chama o método que irá processar a requisição
        /// </summary>
        /// <param name="request">Modelo de Requisição</param>
        /// <param name="ctx">Token de cancelamento</param>
        /// <returns></returns>
        Task IHandler<TRequest>.Handle(TRequest request, CancellationToken ctx)
        {
            var service = (THandler)Services.GetService(typeof(THandler));
            var handler = Handle.Compile().Invoke(service);
            return handler.Invoke(request, ctx);
        }
    }
}
