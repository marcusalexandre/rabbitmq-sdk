namespace RabbitMQ.Configuration
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using RabbitMQ.Client;
    using RabbitMQ.Entities;
    using RabbitMQ.Handlers;
    using RabbitMQ.Serializers;
    using RabbitMQ.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Parâmetrização para funcionamento com o RabbitMq
    /// </summary>
    public class RabbitMqConfiguration
    {
        private readonly List<SubscriberAttribute> _mapping = new List<SubscriberAttribute>();
        private int _delayInSeconds = 30;
        private string _hostname;
        private string _username;
        private string _password;
        private string _virtualHost;
        private int _port;
        private int _responseTimeoutInSeconds;
        private bool _useSSL;
        private int _maxAttempts = 5;
        private bool _shouldCreateScope;

        /// <summary>
        /// Parâmetrização para funcionamento com o RabbitMq
        /// </summary>
        /// <param name="services">Componente para utilização de injeção de dependência</param>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public RabbitMqConfiguration(IServiceCollection services, IConnectionFactory? connectionFactory = null)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
            Services = services;
            Services.AddSingleton(typeof(IPublisher<>), typeof(SingleChannelPublisher<>));
            Services.AddSingleton(typeof(IPublisher<,>), typeof(RabbitMqPublisher<,>));
            Services.AddSingleton(typeof(Serializer), (s) => new Serializer(s));
            Services.AddSingleton<Connection>();

            Services.AddSingleton(this);

            if (connectionFactory != null)
            {
                Services.AddSingleton(connectionFactory);
                return;
            }
            Services.AddSingleton<IConnectionFactory>(delegate
            {
                var connectionFactory = new ConnectionFactory
                {
                    HostName = _hostname,
                    UserName = _username,
                    Password = _password,
                    Port = _port,
                    RequestedHeartbeat = TimeSpan.FromSeconds(60),
                    RequestedChannelMax = 2048,
                    UseBackgroundThreadsForIO = false,
                    VirtualHost = _virtualHost,
                    AutomaticRecoveryEnabled = true
                };
                if (_useSSL)
                {
                    connectionFactory.Ssl = new SslOption
                    {
                        ServerName = _hostname,
                        Enabled = true
                    };
                }
                return connectionFactory;
            });
        }

        /// <summary>
        /// Injeção de Dependencia
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Parametriza manualmente o bloco de código
        /// </summary>
        /// <param name="hostname">Nome do servidor</param>
        /// <param name="username">Nome do usuário de acesso, ele precisa ter acesso ao enfileiramento e ao desenfileiramento</param>
        /// <param name="password">Senha do usuário de acesso</param>
        /// <param name="virtualHost">Nome do Host Virtual</param>
        /// <param name="port">Porta de Comunicação</param>
        /// <param name="responseTimeoutInSeconds">Timeout para recuperação de respostas RPC</param>
        /// <param name="maxAttempts">Máximo de tentativas de processamento, antes de enviar para uma fila morta</param>
        /// <param name="useSSL">Uso de SSL</param>
        /// <returns>Configuração</returns>
        public RabbitMqConfiguration WithParameters(string hostname, string username, string password,
            string virtualHost = "/",
            int port = 5672,
            int responseTimeoutInSeconds = 30,
            int maxAttempts = 5,
            bool useSSL = false)
        {
            _maxAttempts = maxAttempts;
            _delayInSeconds = responseTimeoutInSeconds;
            _hostname = hostname;
            _username = username;
            _password = password;
            _virtualHost = virtualHost;
            _port = port;
            _responseTimeoutInSeconds = responseTimeoutInSeconds;
            _useSSL = useSSL;
            return this;
        }

        /// <summary>
        /// Seta a quantidade de mensagens que serão processadas por vez no subscriber
        /// </summary>
        /// <returns></returns>
        public RabbitMqConfiguration SetPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }

        /// <summary>
        /// Cria um novo scope a cada evento de subscriber da fila
        /// </summary>
        /// <returns></returns>
        public RabbitMqConfiguration WithScope()
        {
            ShouldCreateScope = true;
            return this;
        }
        /// <summary>
        /// Utiliza as váriaveis de ambiente padrão:
        /// RABBITMQ_HOST: Host
        /// RABBITMQ_PORT: Porta
        /// RABBITMQ_USER: Usuário
        /// RABBITMQ_PASSWORD: Senha
        /// RABBITMQ_VHOST: Virtual Host
        /// RABBITMQ_MAXATTEMPS: Máximo de Tentativas
        /// RABBITMQ_RETRYDELAY: Delay entre retentativas
        /// RABBITMQ_USE_SSL: Uso de SSL
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public RabbitMqConfiguration WithEnviroments(IConfiguration config)
        {
            var prefetchCount = config.GetValue<ushort?>("RABBITMQ_PREFETCHCOUNT");
            if (prefetchCount.HasValue)
                SetPrefetchCount(prefetchCount.Value);
            var prefix = config.GetValue<string>("RABBITMQ_NAMEPREFIX");
            var sufix = config.GetValue<string>("RABBITMQ_NAMESUFIX");
            if (!string.IsNullOrEmpty(prefix) || !string.IsNullOrEmpty(sufix))
                SetQueuenameComposition(prefix, sufix ?? "v1");
            var ttl = config.GetValue<string>("RABBITMQ_TTL");
            if (!string.IsNullOrWhiteSpace(ttl) && TimeSpan.TryParse(ttl, out var ttlTime))
                SetTtl(ttlTime);
            var useScope = config.GetValue<bool?>("RABBITMQ_USESCOPE");
            if (useScope.HasValue && useScope.Value)
                WithScope();

            return WithParameters(config.GetValue<string>("RABBITMQ_HOST") ?? "localhost",
                           config.GetValue<string>("RABBITMQ_USER") ?? "guest",
                           config.GetValue<string>("RABBITMQ_PASSWORD") ?? "guest",
                           config.GetValue<string>("RABBITMQ_VHOST") ?? "/",
                           config.GetValue<int?>("RABBITMQ_PORT") ?? 5672,
                           config.GetValue<int?>("RABBITMQ_RETRYDELAY") ?? 30,
                           config.GetValue<int?>("RABBITMQ_MAXATTEMPS") ?? 5,
                           config.GetValue<bool?>("RABBITMQ_USE_SSL") ?? true);
        }

        /// <summary>
        /// Adiciona uma funcionalidade dinâmica ao RabbitMq para filas asincronas
        /// </summary>
        /// <typeparam name="THandler">Handler de subscrição</typeparam>
        /// <typeparam name="TRequest">Modelo de requisição</typeparam>
        /// <param name="handler">Método de processamento</param>
        /// <returns>Configuração</returns>
        public RabbitMqConfiguration AddSubscriber<THandler, TRequest>(Expression<Func<THandler, Func<TRequest, CancellationToken, Task>>> handler)
        {
            Services.AddTransient(typeof(THandler));
            Services.AddTransient<IHandler<TRequest>>(sp => new Handler<THandler, TRequest>(sp, handler));
            Services.AddHostedService<RabbitMqSubscriber<TRequest>>();
            return this;
        }

        /// <summary>
        /// Adiciona uma funcionalidade dinâmica ao RabbitMq para filas asincronas no modelo RPC
        /// </summary>
        /// <typeparam name="THandler">Handler de subscrição</typeparam>
        /// <typeparam name="TRequest">Modelo de requisição</typeparam>
        /// <typeparam name="TResponse">Modelo de resposta</typeparam>
        /// <param name="handler">Método de processamento</param>
        /// <returns>Configuração</returns>
        public RabbitMqConfiguration AddSubscriber<THandler, TRequest, TResponse>(Expression<Func<THandler, Func<TRequest, CancellationToken, Task<TResponse>>>> handler)
        {
            Services.AddTransient(typeof(THandler));
            Services.AddTransient<IHandler<TRequest, TResponse>>(sp => new Handler<THandler, TRequest, TResponse>(sp, handler));
            Services.AddHostedService<RabbitMqSubscriber<TRequest, TResponse>>();
            return this;
        }

        /// <summary>
        /// Define a composição da nomeclatura padrão para filas do rabbitmq
        /// Obs: Somente para definição automática de nomes de filas
        /// </summary>
        /// <param name="prefix">Prefixo</param>
        /// <param name="sufix">Sufixo (Por padrão, indicamos que seja o versionamento da queue, expl: v1)</param>
        /// <param name="deadLetter">Sufixo para Dead-Letter</param>
        /// <returns></returns>
        public RabbitMqConfiguration SetQueuenameComposition(string prefix,
            string sufix = "v1",
            string deadLetter = "dead-letter")
        {
            PrefixName = prefix;
            SufixName = sufix;
            DeadLetterName = deadLetter;
            return this;
        }

        /// <summary>
        /// O RabbitMQ permite que você defina TTL (tempo de vida) para mensagens e filas
        /// </summary>
        /// <param name="ttl">Tempo padrão para mensagens e filas</param>
        public RabbitMqConfiguration SetTtl(TimeSpan ttl)
        {
            Ttl = ttl;
            return this;
        }

        /// <summary>
        /// Define uma quantidade máxima de mensagens que a fila poderá receber.
        /// Caso ultrapasse a quantidade o RabbitMq emitirá o comportamento de estouro de mensagens
        /// </summary>
        /// <param name="maxLenght">Quantidade máxima de mensagens na fila suportado</param>
        public RabbitMqConfiguration SetMessagesMaxLength(int maxLenght)
        {
            MessagesMaxLength = maxLenght;
            return this;
        }

        /// <summary>
        /// Define uma quantidade máxima em bytes que a fila poderá receber.
        /// Caso ultrapasse a quantidade o RabbitMq emitirá o comportamento de estouro de mensagens
        /// </summary>
        /// <param name="maxBytesLenght">Quantidade máxima de bytes na fila suportada</param>
        public RabbitMqConfiguration SetMessagesMaxBytesLength(int maxBytesLenght)
        {
            MessagesBytesMaxLength = maxBytesLenght;
            return this;
        }

        /// <summary>
        /// Adiciona automáticamente subscribers que foram marcados por aspecto com [Subscriber]
        /// Obs: É obrigatório que este Subscriber seja um método Task SomeMethod(Model model, CancellationToken ctx)
        /// onde SomeMethod é qualquer nome de método 
        /// </summary>
        /// <param name="assembly">Assembly de Leitura</param>
        /// <returns>Configuração</returns>
        public RabbitMqConfiguration AddSubscribers(Assembly assembly)
        {
            var mappings = assembly.GetTypes()
                .Where(type => type.IsPublic && type.IsClass)
                .Select(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.GetCustomAttributes<SubscriberAttribute>().Any()))
                .SelectMany(method => method.Select(m => new { method = m, attr = m.GetCustomAttributes<SubscriberAttribute>() }));

            foreach (var mapping in mappings)
            {
                var @class = mapping.method.ReflectedType;
                var request = mapping.method.GetParameters().FirstOrDefault()?.ParameterType ?? throw new NotSupportedException($"{@class.Name}.{mapping.method.Name} não é compativel com Subscriber");
                var response = mapping.method.ReturnType;

                if (request == null || request == typeof(CancellationToken))
                    throw new ArgumentException($"{@class.Name}.{mapping.method.Name} : Corpo da requisição da mensagem éobrigatório.");

                if (mapping.method.GetParameters().LastOrDefault()?.ParameterType != typeof(CancellationToken))
                    throw new ArgumentException($"{@class.Name}.{mapping.method.Name} : CancellationToken é um argumento obrigatório.");

                Services.AddTransient(@class);
                foreach (var attribute in mapping.attr)
                {
                    attribute.Request = request;
                    Type subscriber, handler, handlerImpl;
                    _mapping.Add(attribute);
                    if (response == typeof(Task))
                    {
                        handler = typeof(IHandler<>).MakeGenericType(request);
                        handlerImpl = typeof(Handler<,>).MakeGenericType(@class, request);
                        subscriber = typeof(RabbitMqSubscriber<>).MakeGenericType(request);
                    }
                    else if (response.IsGenericType && response.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        attribute.Response = response.GetGenericArguments().SingleOrDefault();
                        handler = typeof(IHandler<,>).MakeGenericType(request, attribute.Response);
                        handlerImpl = typeof(Handler<,,>).MakeGenericType(@class, request, attribute.Response);
                        subscriber = typeof(RabbitMqSubscriber<,>).MakeGenericType(request, attribute.Response);
                    }
                    else
                    {
                        throw new NotSupportedException($"{@class.Name}.{mapping.method.Name} não é compativel com QueueAttribute");
                    }

                    Services.AddTransient(handler, sp => Activator.CreateInstance(handlerImpl, new object[] { sp, mapping.method }));
                    typeof(ServiceCollectionHostedServiceExtensions)
                        .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), new[] { typeof(IServiceCollection) })
                        .MakeGenericMethod(subscriber)
                        .Invoke(null, new object[] { Services });
                }
                continue;

            }

            return this;
        }


        #region Internals Methods and Properties

        /// <summary>
        /// Se não indicado o nome da fila que será utilizada, adiciona o prefixo a montagem automática do nome da fila
        /// </summary>
        internal string PrefixName { get; private set; } = "queue";

        /// <summary>
        /// Se não indicado o nome da fila que será utilizada, adiciona o sufixo a montagem automática do nome da fila
        /// </summary>
        internal string SufixName { get; private set; } = "v1";

        /// <summary>
        /// Todas as 'filas mortas' são criadas com o sufixo configurado
        /// </summary>
        internal string DeadLetterName { get; private set; } = "dead-letter";

        internal string DelayedName { get; private set; } = "delayed";

        /// <summary>
        /// Define o TTL (tempo de vida) para mensagens e filas
        /// </summary>
        internal TimeSpan? Ttl { get; private set; }

        /// <summary>
        /// Define o comprimento máximo de uma fila baseado na quantidade de um número definido de mensagens 
        /// </summary>
        internal int? MessagesMaxLength { get; private set; }

        /// <summary>
        /// Define o comprimento máximo de uma fila baseado na quantidade de um número definido de bytes que foram adicionados a fila 
        /// </summary>
        internal int? MessagesBytesMaxLength { get; private set; }

        /// <summary>
        /// Permite que as instâncias relacionadas as filas sejam criadas em um contexto de escopo (Scope) em detrimento 
        /// ao Transient que é o comportamento padrão
        /// </summary>
        internal bool ShouldCreateScope { get; private set; }

        /// <summary>
        /// TTL das retentativas
        /// </summary>
        internal TimeSpan GetDelayInSeconds() => TimeSpan.FromSeconds(_delayInSeconds);

        /// <summary>
        /// Timeout do RPC
        /// </summary>
        internal TimeSpan GetResponseTimeout() => TimeSpan.FromSeconds(_responseTimeoutInSeconds);

        /// <summary>
        /// Máximo de tentativas
        /// </summary>
        internal int GetMaxAttempts() => _maxAttempts;

        /// <summary>
        /// Prefetch Mensagens
        /// </summary>
        internal ushort PrefetchCount { get; private set; } = 5;

        /// <summary>
        /// Obtém o mapeamento asíncrono pelo subscriber associado 
        /// </summary>
        /// <typeparam name="TRequest">Modelo de requisição</typeparam>
        /// <returns>Configuração de Subscriber</returns>
        internal SubscriberAttribute GetMaping<TRequest>()
        {
            return _mapping.SingleOrDefault(t => t.Request == typeof(TRequest) && t.Response == null);
        }

        /// <summary>
        /// Obtém o mapeamento asíncrono pelo subscriber associado 
        /// </summary>
        /// <typeparam name="TRequest">Modelo de requisição</typeparam>
        /// <typeparam name="TResponse">Modelo de resposta</typeparam>
        /// <returns>Configuração de Subscriber</returns>
        internal SubscriberAttribute GetMaping<TRequest, TResponse>()
        {
            return _mapping.SingleOrDefault(t => t.Request == typeof(TRequest) && t.Response == typeof(TResponse));
        }
        #endregion
    }
}
