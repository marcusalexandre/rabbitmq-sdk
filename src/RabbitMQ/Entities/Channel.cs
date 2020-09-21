namespace RabbitMQ.Entities
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Configuration;
    using RabbitMQ.Handlers;
    using RabbitMQ.Serializers;
    using RabbitMQ.ValueObjects;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Abstração do canal de Comunicação com o RabbitMQ
    /// </summary>
    public abstract class Channel : IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly IModel _model;
        private readonly Serializer _serializer;
        private readonly EventingBasicConsumer _consumer;
        private readonly BlockingCollection<BasicDeliverEventArgs> _blockingCollection;
        private readonly RabbitMqConfiguration _config;
        private readonly EventingBasicConsumer _eventingBasicConsumer;
        private ILogger? _logger;
        private bool _disposed;

        /// <summary>
        /// Abstração do canal de Comunicação com o RabbitMQ
        /// </summary>
        /// <param name="services">Provedor de serviços</param>
        /// <param name="model">RabbitMq model</param>
        /// <param name="descriptor">Descrição da fila</param>
        /// <param name="serializer">Encapsulamento do serializador</param>
        /// <param name="blockingCollection">Bloqueio para utilização de RPC</param>
        /// <param name="config">Configurações do framework</param>
        protected Channel(
            IServiceProvider services,
            IModel model,
            QueueDescriptor descriptor,
            Serializer serializer,
            BlockingCollection<BasicDeliverEventArgs> blockingCollection,
            RabbitMqConfiguration config)
        {
            _services = services;
            Descriptor = descriptor;
            var loggerFactory = (ILoggerFactory)_services.GetService(typeof(ILoggerFactory));
            _blockingCollection = blockingCollection;
            _config = config;
            _model = model;
            _serializer = serializer;
            _consumer = new EventingBasicConsumer(_model);
            _eventingBasicConsumer = new EventingBasicConsumer(model);
            _logger = loggerFactory?.CreateLogger<Channel>();
        }

        /// <summary>
        /// Descrição da Query
        /// </summary>
        public QueueDescriptor Descriptor { get; }

        /// <summary>
        /// RabbitMq BasicProperties
        /// </summary>
        /// <param name="attempt">Numero da tentativa</param>
        /// <param name="ttl">Tempo de duração da fila</param>
        /// <returns>BasicProperties</returns>
        private IBasicProperties GetBasicProperties(int attempt, TimeSpan? ttl)
        {

            var properties = _model.CreateBasicProperties();
            properties.ContentType = MediaTypeNames.Application.Json;
            properties.DeliveryMode = 2;
            properties.CorrelationId = string.Empty;
            properties.Headers = new Dictionary<string, object>
            {
                ["x-attempt"] = attempt,
            };
            ttl ??= _config.Ttl;
            if (ttl != null)
            {
                properties.Headers["x-message-ttl"] = (int)ttl.Value.TotalMilliseconds;
                properties.Expiration = ((int)ttl.Value.TotalMilliseconds).ToString();
            }
            return properties;
        }

        /// <summary>
        /// Publisher
        /// </summary>
        protected TResponse Publish<TRequest, TResponse>(TRequest request, TimeSpan? ttl)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!(request is Exception) && Descriptor.IsAsync)
            {
                throw new NotSupportedException();
            }

            _consumer.Received += OnConsumerOnReceived;
            var requestBytes = _serializer.Serialize(request);
            var properties = GetBasicProperties(1, ttl);
            properties.CorrelationId = Guid.NewGuid().ToString();
            properties.ReplyTo = _model.QueueDeclare().QueueName;
            _model.BasicConsume(properties.ReplyTo, true, _consumer);
            _model.BasicPublish(string.Empty, Descriptor.QueueName, properties, requestBytes);

            BasicDeliverEventArgs args;
            try
            {
                var ctx = new CancellationTokenSource();
                Task.Delay(_config.GetResponseTimeout(), CancellationToken.None).ContinueWith(c => ctx.Cancel(), CancellationToken.None);
                args = _blockingCollection.Take(ctx.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException();
            }

            if (true != args.BasicProperties?.Headers?.ContainsKey(nameof(Exception)))
            {
                var response = _serializer.Deserialize(typeof(TResponse), args.Body);
                return (TResponse)response;
            }

            var exception = _serializer.Deserialize(typeof(Exception), args.Body) as Exception ?? new SystemException();
            throw exception;
        }

        /// <summary>
        /// Recuperação da resposta da fila
        /// </summary>
        /// <param name="sender">envio</param>
        /// <param name="ea">informações de resposta</param>
        private void OnConsumerOnReceived(object? sender, BasicDeliverEventArgs ea)
        {
            _blockingCollection.Add(ea);
        }

        /// <summary>
        /// Publisher
        /// </summary>
        protected void Publish<TRequest>(TRequest request, int attempt = 0, TimeSpan? ttl = null, TimeSpan? enqueueTime = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!(request is Exception) && !Descriptor.IsAsync)
            {
                throw new NotSupportedException();
            }

            if (enqueueTime.HasValue)
                ttl = enqueueTime;

            var properties = GetBasicProperties(attempt, ttl);
            var requestBytes = _serializer.Serialize(request);
            Publish(attempt, enqueueTime, properties, requestBytes);
        }

        private void Publish(int attempt, TimeSpan? enqueueTime, IBasicProperties properties, byte[] requestBytes)
        {
            string queue;
            if (attempt >= _config.GetMaxAttempts())
            {
                properties.Headers.TryGetValue("x-dead-queue-attempt", out var deadQueueAttempt);
                var attempts = deadQueueAttempt == null
                    ? attempt
                    : attempt + (int)properties.Headers["x-dead-queue-attempt"];

                properties = GetBasicProperties(0, null);
                properties.Headers["x-dead-queue-attempt"] = attempts;
                queue = Descriptor.DeadLetterQueueName;
            }
            else if (enqueueTime.HasValue)
            {
                queue = Descriptor.DelayedQueueName;
                DeclareDelayedQueue();
            }
            else
            {
                queue = Descriptor.QueueName;
            }

            _model.BasicPublish(string.Empty, queue, properties, requestBytes);
        }

        private void DeclareDelayedQueue()
        {
            IDictionary<string, object> props = new Dictionary<string, object>()
                {
                    { "x-dead-letter-exchange", "" },
                    { "x-dead-letter-routing-key", Descriptor.QueueName }
                };
            _model.QueueDeclare(Descriptor.DelayedQueueName, true, false, true, props);
        }

        /// <summary>
        /// Publica a resposta
        /// </summary>
        private void PublishResponse<T>(BasicDeliverEventArgs e, T response) where T : new()
        {
            var properties = GetBasicProperties(1, _config.GetResponseTimeout());
            var requestBytes = _serializer.Serialize(response ?? new object());
            var props = e.BasicProperties;
            properties.CorrelationId = props.CorrelationId;
            if (response is Exception)
            {
                properties.Headers = new Dictionary<string, object>
                 {
                     {nameof(Exception), response.GetType().AssemblyQualifiedName}
                 };
            }
            _model.BasicPublish(string.Empty, props.ReplyTo, properties, requestBytes);
        }

        /// <summary>
        /// Subscriber
        /// </summary>
        protected void Subscribe<TRequest, TResponse>(CancellationToken ctx)
        {
            _eventingBasicConsumer.Received += async (sender, e) =>
            {
                try
                {
                    var @event = (TRequest)_serializer.Deserialize(typeof(TRequest), e.Body);
                    object? response;

                    try
                    {

                        if (_config.ShouldCreateScope)
                        {
                            using (var scope = _services.CreateScope())
                            {
                                response = await CallService<TRequest, TResponse>(@event, scope.ServiceProvider, ctx);
                            }
                        }
                        else
                        {
                            response = await CallService<TRequest, TResponse>(@event, _services, ctx);
                        }
                    }
                    catch (Exception exception)
                    {
                        response = exception;
                    }
                    PublishResponse(e, response);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, ex.Message);
                }
                finally
                {
                    TryAck(e);
                }
            };

            _model.ConfirmSelect();
            _model.ExchangeDeclare(Descriptor.ExchangeName, ExchangeType.Fanout, true, true,
                new Dictionary<string, object>
                {
                    ["x-delayed-type"] = ExchangeType.Direct
                });


            _model.QueueBind(Descriptor.QueueName, Descriptor.ExchangeName, string.Empty, null);
            _model.BasicConsume(Descriptor.QueueName, false, _eventingBasicConsumer);
        }

        /// <summary>
        /// Subscriber
        /// </summary>
        protected void Subscribe<TRequest>(CancellationToken ctx)
        {
            _eventingBasicConsumer.Received += async (sender, e) =>
            {
                object? attemptObj = null;
                e.BasicProperties.Headers?.TryGetValue("x-attempt", out attemptObj);
                int.TryParse(attemptObj?.ToString() ?? "0", out var attempt);

                try
                {
                    var request = _serializer.Deserialize(typeof(TRequest), e.Body);
                    var @event = (TRequest)request;
                    await ProcessMessage(@event, ctx);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, ex.Message);

                    object? ttlObj = null;
                    e.BasicProperties.Headers?.TryGetValue("x-message-ttl", out ttlObj);
                    var ttl = ttlObj == null
                        ? _config.GetDelayInSeconds()
                        : TimeSpan.FromMilliseconds((int)ttlObj);

                    var properties = GetBasicProperties(++attempt, ttl);
                    Publish(attempt, ttl, properties, e.Body.ToArray());
                }
                finally
                {
                    TryAck(e);
                }
            };

            ConfigureSubscriber();
        }

        private void ConfigureSubscriber()
        {
            _model.ConfirmSelect();
            _model.ExchangeDeclare(Descriptor.ExchangeName, ExchangeType.Fanout, true, true,
                new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = Descriptor.DeadLetterExchangeName,
                    ["x-delayed-type"] = ExchangeType.Direct
                });
            _model.ExchangeDeclare(Descriptor.DeadLetterExchangeName, ExchangeType.Fanout, true, true,
                new Dictionary<string, object>());

            _model.QueueBind(Descriptor.QueueName, Descriptor.ExchangeName, string.Empty, null);
            _model.QueueBind(Descriptor.DeadLetterQueueName, Descriptor.DeadLetterExchangeName, string.Empty, null);
            _model.BasicConsume(Descriptor.QueueName, false, _eventingBasicConsumer);
        }

        private async Task ProcessMessage<TRequest>(TRequest @event, CancellationToken ctx)
        {
            if (_config.ShouldCreateScope)
            {
                using (var scope = _services.CreateScope())
                {
                    await CallService(@event, scope.ServiceProvider, ctx);
                }
            }
            else
            {
                await CallService(@event, _services, ctx);
            }
        }

        /// <summary>
        /// Tenta realizar o Ack
        /// </summary>
        /// <param name="e">BasicDeliverEventArgs</param>
        private void TryAck(BasicDeliverEventArgs e)
        {
            try
            {
                _model.BasicAck(e.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, ex.Message);
            }
        }

        /// <summary>
        /// Chama o handler de processamento Rpc
        /// </summary>
        private static async Task<TResponse> CallService<TRequest, TResponse>(TRequest @event, IServiceProvider currentService, CancellationToken ctx)
        {
            var requestHandler = (IHandler<TRequest, TResponse>)currentService.GetService(typeof(IHandler<TRequest, TResponse>));
            return await requestHandler.Handle(@event, ctx);
        }

        /// <summary>
        /// Chama o handler de processamento 
        /// </summary>
        private static async Task CallService<TRequest>(TRequest @event, IServiceProvider currentService, CancellationToken ctx)
        {
            var requestHandler = (IHandler<TRequest>)currentService.GetService(typeof(IHandler<TRequest>));
            await requestHandler.Handle(@event, ctx);
        }

        /// <summary>
        /// Retorna verdadeiro se o canal esta aberto
        /// </summary>
        public bool IsConnected => _model != null && _model.IsOpen && !_disposed;

        /// <summary>
        /// Retorna verdadeiro se o canal esta fechado
        /// </summary>
        public bool IsClosed => !IsConnected;

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _model?.Dispose();
            _blockingCollection.Dispose();
            _disposed = true;
        }
    }
}