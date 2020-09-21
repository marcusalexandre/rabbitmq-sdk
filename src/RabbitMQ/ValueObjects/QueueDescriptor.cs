namespace RabbitMQ.ValueObjects
{
    using Microsoft.AspNetCore.Mvc;
    using RabbitMQ.Configuration;
    using System;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Padronização para geração de nomes de filas
    /// </summary>
    public struct QueueDescriptor
    {
        private const char _queueNameSeparator = '.';

        /// <summary>
        /// Padronização para geração de nomes de filas
        /// </summary>
        /// <param name="config">Configuração</param>
        /// <param name="attr">Descrição do Subscriber</param>
        /// <param name="queueName">Nome da fila, se houver</param>
        /// <param name="requestType">Tipo de Requisição</param>
        /// <param name="responseType">Tipo de resposta RPC, se houver</param>
        public QueueDescriptor(
            RabbitMqConfiguration config,
            SubscriberAttribute? attr,
            string? queueName,
            Type requestType,
            Type? responseType = null)
        {
            RequestType = requestType;
            ResponseType = responseType;
            IsAsync = responseType == null;
            QueueName = queueName ?? attr?.Queue ?? BuildName(requestType, responseType, config.PrefixName, config.SufixName);
            ExchangeName = attr?.Exchange ?? BuildName(requestType, responseType, config.PrefixName, config.SufixName);
            DeadLetterExchangeName = $"{ExchangeName}{_queueNameSeparator}{config.DeadLetterName}";
            DeadLetterQueueName = $"{QueueName}{_queueNameSeparator}{config.DeadLetterName}";
            DelayedQueueName = $"{QueueName}{_queueNameSeparator}{config.DelayedName}";
            DelayedQueueExchangeName = $"{QueueName}{_queueNameSeparator}{config.DelayedName}";
        }


        /// <summary>
        /// Monta o nome da fila de maneira automática, baseada no modelo e nos sufixos e prefixos configurados
        /// </summary>
        /// <param name="requestType">Tipo de requisição</param>
        /// <param name="responseType">Tipo de resposta</param>
        /// <param name="prefix">Prefixo de composição</param>
        /// <param name="sufix">Sufixo de composição</param>
        /// <returns>Nome da fila</returns>
        private static string BuildName(
            Type requestType,
            Type? responseType,
            string prefix,
            string sufix)
        {
            var name = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                name.Append(prefix);
                name.Append(_queueNameSeparator);
            }
            name.Append(GetTypeFriendlyName(requestType));
            if (responseType != null)
            {
                name.Append("-");
                name.Append(GetTypeFriendlyName(responseType));
            }
            if (!string.IsNullOrWhiteSpace(sufix))
            {
                name.Append(_queueNameSeparator);
                name.Append(sufix);
            }

            return name.ToString().ToLower();
        }


        /// <summary>
        /// Monta um nome amigável para nomear automáticamente uma fila pelo modelo utilizado
        /// </summary>
        /// <param name="type">Tipo do modelo da fila</param>
        /// <returns>String builder contendo o texto</returns>
        private static StringBuilder GetTypeFriendlyName(Type type)
        {
            var name = new StringBuilder();
            string typeName;

            if (type.IsGenericType)
            {
                typeName = type.Name.Substring(0, type.Name.IndexOf('`'));
            }
            else
            {
                typeName = type.Name;
            }

            if (type.IsInterface && type.Name.StartsWith("i", StringComparison.InvariantCultureIgnoreCase))
            {
                name.Append(typeName.Substring(1));
            }
            else
            {
                name.Append(typeName);
            }

            if (type.IsGenericType)
            {
                name.Append("-");
                name.Append(GetTypeFriendlyName(type.GetGenericArguments().First()));
            }

            return name;
        }

        /// <summary>
        /// Nome do Exchange no RabbitMQ
        /// </summary>
        public string? ExchangeName { get; }

        /// <summary>
        /// Nome da Fila no RabbitMQ
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// Nome da Fila Morta
        /// </summary>
        public string DeadLetterQueueName { get; }

        /// <summary>
        /// Nome do Correio para Fila MOrta
        /// </summary>
        public string DeadLetterExchangeName { get; }

        /// <summary>
        /// Nome da Fila Espera
        /// </summary>
        public string DelayedQueueName { get; }

        /// <summary>
        /// Nome do Correio para Fila MOrta
        /// </summary>
        public string DelayedQueueExchangeName { get; }

        /// <summary>
        /// Verdadeiro se a fila é asincrona
        /// </summary>
        public bool IsAsync { get; }

        /// <summary>
        /// Tipo de modelo de requisição para serialização
        /// </summary>
        public Type RequestType { get; }

        /// <summary>
        /// Tipo de modelo da resposta  para serialização
        /// </summary>
        public Type? ResponseType { get; }

    }
}