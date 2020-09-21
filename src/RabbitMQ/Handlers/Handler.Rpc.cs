namespace RabbitMQ.Handlers
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Fachada de implementação de Handler RPC, para definição por atributos
    /// </summary>
    /// <typeparam name="THandler">Tipo do handler</typeparam>
    /// <typeparam name="TRequest">Tipo do modelo de requisição</typeparam>
    /// <typeparam name="TResponse">Tipo do modelo de resposta</typeparam>
    public class Handler<THandler, TRequest, TResponse> : IHandler<TRequest, TResponse>
    {
        public IServiceProvider Services { get; }
        public Expression<Func<THandler, Func<TRequest, CancellationToken, Task<TResponse>>>> Handle { get; }

        public Handler(IServiceProvider services, Expression<Func<THandler, Func<TRequest, CancellationToken, Task<TResponse>>>> handle)
        {
            Services = services;
            Handle = handle;
        }

        public Handler(IServiceProvider services, MethodInfo methodInfo)
            : this(services, h => (req, ctx) => (Task<TResponse>)methodInfo.Invoke(h, new object[] { req, ctx }))
        {

        }

        Task<TResponse> IHandler<TRequest, TResponse>.Handle(TRequest request, CancellationToken ctx)
        {
            var service = (THandler)Services.GetService(typeof(THandler));
            var handler = Handle.Compile().Invoke(service);
            return handler.Invoke(request, ctx);
        }
    }
}
