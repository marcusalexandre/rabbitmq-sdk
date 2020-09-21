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
    /// Canal de Comunicação com o RabbitMQ - Asincrono
    /// </summary>
    public class Channel<TRequest> : Channel
    {

        /// <summary>
        /// Canalde comunicação como RabbitMQ
        /// </summary>
        /// <param name="services">netcore IoC</param>
        /// <param name="model">RabbitMq Model</param>
        /// <param name="serializer">Encapsulamento de serialzador</param>
        /// <param name="config">Configuração do Serviço</param>
        /// <param name="attr">Parametrização do Subscriber</param>
        /// <param name="queueName">Nome da Query</param>
        public Channel(IServiceProvider services,
            IModel model,
            Serializer serializer,
            RabbitMqConfiguration config,
            SubscriberAttribute? attr = default,
            string? queueName = null)
            : base(services, model, new QueueDescriptor(config, attr, queueName, typeof(TRequest)), serializer, new BlockingCollection<BasicDeliverEventArgs>(), config)
        {

        }

        /// <summary>
        /// Subscrição de eventos
        /// </summary>
        /// <param name="ctx">Token de cancelamento</param>
        public void Subscribe(CancellationToken ctx) => Subscribe<TRequest>(ctx);

        /// <summary>
        /// Publisher
        /// </summary>
        public Task Publish(TRequest request, CancellationToken cancellationToken, TimeSpan? enqueueTime = null)
        {
            Publish(request, enqueueTime: enqueueTime);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}