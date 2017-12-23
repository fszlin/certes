using System;
using System.Collections.Generic;
using System.Linq;

namespace Certes.Acme
{
    /// <summary>
    /// Represents a ACME entity returned from the server.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class AcmeResult<T>
    {
        /// <summary>
        /// Gets or sets the entity data.
        /// </summary>
        /// <value>
        /// The entity data.
        /// </value>
        public T Data { get; set; }

        /// <summary>
        /// Gets or sets the raw HTTP content.
        /// </summary>
        /// <value>
        /// The raw HTTP content.
        /// </value>
        public byte[] Raw { get; set; }

        /// <summary>
        /// Gets or sets the entity location.
        /// </summary>
        /// <value>
        /// The entity location.
        /// </value>
        public Uri Location { get; set; }

        /// <summary>
        /// Gets or sets the links.
        /// </summary>
        /// <value>
        /// The links.
        /// </value>
        public IList<RelLink> Links { get; set; }

        /// <summary>
        /// Gets or sets the content type header.
        /// </summary>
        /// <value>
        /// The content type header.
        /// </value>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the JSON response.
        /// </summary>
        /// <value>
        /// The JSON response.
        /// </value>
        public string Json { get; set; }
    }

    /// <summary>
    /// Helper methods for <see cref="AcmeResult{T}"/>
    /// </summary>
    public static class AcmeResultExtensions
    {
        /// <summary>
        /// Gets the terms of service URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result">The result.</param>
        /// <returns>The terms of service URI</returns>
        public static Uri GetTermsOfServiceUri<T>(this AcmeResult<T> result)
        {
            return result.Links.FirstOrDefault(l => l.Rel == "terms-of-service")?.Uri;
        }
    }
}
