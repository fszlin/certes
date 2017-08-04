namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the type for <see cref="AuthorizationIdentifierChallenge"/>.
    /// </summary>
    public static class AuthorizationIdentifierChallengeTypes
    {
        /// <summary>
        /// The http-01 challenge.
        /// </summary>
        public const string Http01 = "http-01";

        /// <summary>
        /// The tls-sni-02 challenge.
        /// </summary>
        public const string TlsSni02 = "tls-sni-02";

        /// <summary>
        /// The dns-01 challenge.
        /// </summary>
        public const string Dns01 = "dns-01";
    }
}
