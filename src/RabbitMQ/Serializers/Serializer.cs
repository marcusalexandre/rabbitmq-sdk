namespace RabbitMQ.Serializers
{
    using Newtonsoft.Json;
    using RabbitMQ.ValueObjects;
    using System;
    using System.Text;

    /// <summary>
    /// Proxy para serialização do trafego de dados com o RabbitMQ
    /// No momento estamos trabalhando com JSON, a intenção é:
    ///     * Havendo a necessidade vamos parametrizar esta classe para uso de outros modelos de serialização
    /// </summary>
    public class Serializer : IDisposable
    {
        private static readonly char _exceptionKey = (char)247;
        private static readonly JsonSerializerSettings _options = new JsonSerializerSettings();
        private readonly IServiceProvider _services;

        public Serializer(IServiceProvider services)
        {
            _services = services;
        }

        /// <summary>
        /// Serializa um evento de dominio em dados
        /// </summary>
        /// <param name="object">Modelo de transporte</param>
        /// <returns>bytes[]</returns>
        public byte[] Serialize(object @object)
        {
            bool isException;

            if (isException = @object is Exception)
            {
                @object = new ErrorModel((Exception)@object);
            }
            var @string = JsonConvert.SerializeObject(@object, _options);
            if (isException)
            {
                @string = string.Concat(_exceptionKey, @string);
            }
            var bytes = Encoding.UTF8.GetBytes(@string);
            return bytes;
        }


        /// <summary>
        /// Deserializa um evento de dominio
        /// </summary>
        /// <param name="bytes">Stream</param>
        /// <typeparam name="TResponse">Tipo da resposta</typeparam>
        /// <returns>Modelo de resposta</returns>
        public object Deserialize(Type type, ReadOnlyMemory<byte> bytes)
        {
            var @string = Encoding.UTF8.GetString(bytes.ToArray());
            if (@string.StartsWith(_exceptionKey))
            {
                var error = JsonConvert.DeserializeObject<ErrorModel>(@string.Substring(1), _options);
                return error?.GetException() ?? new Exception();
            }
            if (type.IsInterface)
            {
                type = _services.GetService(type).GetType();
            }
            var @object = JsonConvert.DeserializeObject(@string, type, _options);
            return @object ?? Activator.CreateInstance(type);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {

        }
    }
}