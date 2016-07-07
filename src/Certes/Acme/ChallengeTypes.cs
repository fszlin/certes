using System;

namespace Certes.Acme
{
    /// <summary>
    /// The ACME challenge types.
    /// </summary>
    public static class ChallengeTypes
    {
        /// <summary>
        /// The HTTP challenge. version 1.
        /// </summary>
        public const string Http01 = "http-01";

        /// <summary>
        /// The DNS challenge. version 1.
        /// </summary>
        public const string Dns01 = "dns-01";

        /// <summary>
        /// The TLS with Server Name Indication challenge. version 2.
        /// </summary>
        public const string TlsSni02 = "tls-sni-02";

        /// <summary>
        /// The TLS with Server Name Indication challenge. version 1.
        /// </summary>
        [Obsolete]
        public const string TlsSni01 = "tls-sni-01";
    }
}
