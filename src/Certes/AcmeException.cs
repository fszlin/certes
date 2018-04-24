using System;
using System.Runtime.Serialization;

namespace Certes
{
    /// <summary>
    /// The exception that is thrown when an error occurs while processing ACME operations.
    /// </summary>
    /// <seealso cref="Exception" />
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class AcmeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeException"/> class.
        /// </summary>
        public AcmeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AcmeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeException"/> class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, 
        /// or a null reference (Nothing in Visual Basic) if no inner
        /// exception is specified.
        /// </param>
        public AcmeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !NETSTANDARD1_3
        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeException"/> class.
        /// </summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that
        /// holds the serialized object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> 
        /// that contains contextual information about the source or destination.
        /// </param>
        protected AcmeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> 
        /// with information about the exception.
        /// </summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that
        /// holds the serialized object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> that 
        /// contains contextual information about the source or destination.
        /// </param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
            => base.GetObjectData(info, context);
#endif
    }
}
