namespace RabbitMQ.ValueObjects
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Modelo de Serialização de exceções
    /// </summary>
    [Serializable]
    public class ErrorModel
    {
        /// <summary>
        /// Mensagem de Exceção 
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Trace
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// Tipo da Exceção
        /// </summary>
        public string TypeId { get; set; }

        /// <summary>
        /// Modelo de Erro
        /// </summary>
        public ErrorModel()
        {
            Message = StackTrace = TypeId = string.Empty;
        }

        /// <summary>
        /// Modelo de Erro baseado na exceção
        /// </summary>
        /// <param name="ex">Exceção</param>
        public ErrorModel(Exception ex)
        {
            Message = ex.Message;
            StackTrace = ex.StackTrace;
            TypeId = ex.GetType().AssemblyQualifiedName;
        }

        /// <summary>
        /// Monta a exception por reflexão
        /// </summary>
        public Exception GetException()
        {
            var exception = (Exception)Activator.CreateInstance(Type.GetType(TypeId));
            typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(exception, Message);
            typeof(Exception).GetField("_stackTraceString", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(exception, StackTrace);
            return exception;
        }
    }
}