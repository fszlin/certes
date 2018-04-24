using System;
using System.Runtime.Serialization;
using Certes.Acme;
using Certes.Json;
using Newtonsoft.Json;

namespace Certes
{
    /// <summary>
    /// The exception that is thrown when an error occurs while processing ACME operations.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class AcmeRequestException : Exception
    {
        /// <summary>
        /// The json serializer settings for converting additional information.
        /// </summary>
        private static readonly JsonSerializerSettings jsonSerializerSettings = JsonUtil.CreateSettings();

        /// <summary>
        /// Gets the error occurred while processing ACME operations.
        /// </summary>
        /// <value>
        /// The error occurred while processing ACME operations.
        /// </value>
        public AcmeError Error { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeRequestException"/> class.
        /// </summary>
        public AcmeRequestException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeRequestException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AcmeRequestException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeRequestException"/> class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, 
        /// or a null reference (Nothing in Visual Basic) if no inner
        /// exception is specified.
        /// </param>
        public AcmeRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeRequestException"/> class.
        /// </summary>
        /// <param name="info">
        /// The <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> that
        /// holds the serialized object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="T:System.Runtime.Serialization.StreamingContext"></see> 
        /// that contains contextual information about the source or destination.
        /// </param>
        protected AcmeRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            var errorJson = info.GetString("acme.error");
            if (!string.IsNullOrWhiteSpace(errorJson))
            {
                Error = JsonConvert.DeserializeObject<AcmeError>(errorJson, jsonSerializerSettings);
            }
        }

        /// <summary>
        /// Sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> 
        /// with information about the exception.
        /// </summary>
        /// <param name="info">
        /// The <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> that
        /// holds the serialized object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="T:System.Runtime.Serialization.StreamingContext"></see> that 
        /// contains contextual information about the source or destination.
        /// </param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (Error != null)
            {
                info.AddValue("acme.error", JsonConvert.SerializeObject(Error, jsonSerializerSettings));
            }
        }
    }
}
