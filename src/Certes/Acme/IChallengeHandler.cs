using System;
using System.Collections.Generic;
using System.Text;

namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    public interface IChallengeHandler
    {
        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        string Type { get; }

        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        Uri Url { get; }

        /// <summary>
        /// Gets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        string Token {get;}
    }
}
