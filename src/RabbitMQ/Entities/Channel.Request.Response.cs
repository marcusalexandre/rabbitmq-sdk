namespace RabbitMQ.Entities
{
    using Microsoft.AspNetCore.Mvc;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Configuration;
    using RabbitMQ.Serializers;
    using RabbitMQ.ValueObjects;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Canal de Comunicação com o RabbitMQ - Sincrono
    /// </summary>
    public class Channel<TRequest, TResponse> : Channel
    {
        /// <summary>
        /// Canal de Comunicação com o RabbitMQ - Sincrono
        /// </summary>
        /// <param name="services">netcore IoC</param>
        /// <param name="model">RabbitMq Model</param>
        /// <param name="serializer">Encapsulamento de serialzador</param>
        /// <param name="blockingCollection">Bloqueador de task para aguardo de resposta</param>
        /// <param name="config">Configuração do Serviço</param>
        /// <param name="attr">Parametrização do Subscriber</param>
        /// <param name="queueName">Nome da Query</param>
        public Channel(
            IServiceProvider services,
            IModel model,
            Serializer serializer,
            BlockingCollection<BasicDeliverEventArgs> blockingCollection,
            RabbitMqConfiguration config,
            SubscriberAttribute? attr = default,
            string? queueName = null)
            : base(services, model, new QueueDescriptor(config, attr, queueName, typeof(TRequest), typeof(TResponse)), serializer, blockingCollection, config)
        {

        }

        /// <summary>
        /// Subscrição de eventos
        /// </summary>
        /// <param name="ctx">Token de cancelamento</param>
        public void Subscribe(CancellationToken ctx) => Subscribe<TRequest, TResponse>(ctx);

        /// <summary>
        /// Publica um evento e aguarda a resposta sincronamente em uma exclusiva
        /// </summary>
        /// <param name="event">Evento</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <param name="ttl">Tempo para expiração</param>
        /// <returns>Resposta</returns>
        public Task<TResponse> Publish(TRequest @event, CancellationToken cancellationToken,
            TimeSpan? ttl = null)
        {
            var response = Publish<TRequest, TResponse>(@event, ttl);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(response);
        }
    }
}