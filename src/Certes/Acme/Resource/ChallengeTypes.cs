namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the type for <see cref="Challenge"/>.
    /// </summary>
    public static class ChallengeTypes
    {
        /// <summary>
        /// The http-01 challenge.
        /// </summary>
        public const string Http01 = "http-01";

        /// <summary>
        /// The dns-01 challenge.
        /// </summary>
        public const string Dns01 = "dns-01";

        /// <summary>
        /// The tls-alpn-01 challenge.
        /// </summary>
        public const string TlsAlpn01 = "tls-alpn-01";
    }
}
