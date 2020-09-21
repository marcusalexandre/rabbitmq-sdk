namespace RabbitMQ.Configuration
{
    using Microsoft.Extensions.DependencyInjection;

    public static class RabbitMqExtensions
    {

        /// <summary>
        /// Configura a utilização do RabbitMQ
        /// </summary>
        /// <param name="services">Service Collection</param>
        public static RabbitMqConfiguration AddRabbitMq(this IServiceCollection services)
        {
            return new RabbitMqConfiguration(services);
        }
    }
}
