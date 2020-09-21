namespace Microsoft.AspNetCore.Mvc
{
    using System;

    /// <summary>
    /// Mapeamento da Queue
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class SubscriberAttribute : Attribute
    {

        /// <summary>
        /// Mapeamento da Queue
        /// </summary>
        public SubscriberAttribute(string? queue = null, string? exchange = null)
        {
            Queue = queue;
            Exchange = exchange ?? queue;
        }

        /// <summary>
        /// Nome da Query
        /// </summary>
        public string? Queue { get; }

        /// <summary>
        /// Nome da Exchange
        /// </summary>
        public string? Exchange { get; }

        /// <summary>
        /// Tipo de Requisição
        /// </summary>
        internal Type Request { get; set; }

        /// <summary>
        /// Tipo de Resposta
        /// </summary>
        internal Type Response { get; set; }
    }
}
