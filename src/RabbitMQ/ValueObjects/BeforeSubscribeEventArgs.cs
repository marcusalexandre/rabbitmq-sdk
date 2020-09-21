namespace RabbitMQ.ValueObjects
{
    using System;

    /// <summary>
    /// Argumentos para utilização de evento para executar regra antes de subscrever um
    /// evento
    /// </summary>
    public class BeforeSubscribeEventArgs : EventArgs
    {
        /// <summary>
        /// Argumentos para utilização de evento para executar regra antes de subscrever um
        /// </summary>
        /// <param name="descriptor">Descritor da Queue</param>
        public BeforeSubscribeEventArgs(QueueDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        /// <summary>
        /// Descritor da Fila
        /// </summary>
        public QueueDescriptor Descriptor { get; }
    }
}
