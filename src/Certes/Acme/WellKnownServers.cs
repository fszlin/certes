using System;

namespace Certes.Acme
{
    /// <summary>
    /// The well known ACME servers.
    /// </summary>
    public class WellKnownServers
    {
        /// <summary>
        /// Gets the URI for Let's Encrypt production server.
        /// </summary>
        /// <value>
        /// The URI for Let's Encrypt production server.
        /// </value>
        public static Uri LetsEncrypt { get; } = new Uri("https://acme-v01.api.letsencrypt.org/directory");

        /// <summary>
        /// Gets the URI for Let's Encrypt staging server.
        /// </summary>
        /// <value>
        /// The URI for Let's Encrypt staging server.
        /// </value>
        public static Uri LetsEncryptStaging { get; } = new Uri("https://acme-staging.api.letsencrypt.org/directory");
    }
}
