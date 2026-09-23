namespace Certes.Pkcs
{
    /// <summary>
    /// The algorithms used to protect the private key and certificates in a PFX.
    /// </summary>
    public enum PfxEncryption
    {
        /// <summary>
        /// PBES2 with AES-256-CBC and PBKDF2 using HMAC-SHA256, for both the private key and
        /// the certificates. Readable by OpenSSL 3, current .NET, Windows 10 1709 and later,
        /// Windows Server 2019 and later, macOS, and Android.
        /// </summary>
        Aes256 = 0,

        /// <summary>
        /// The algorithms used before 4.0: 3DES (<c>pbeWithSHAAnd3-KeyTripleDES-CBC</c>) for the
        /// private key and 40-bit RC2 (<c>pbeWithSHAAnd40BitRC2-CBC</c>) for the certificates.
        /// These are weak and are rejected by OpenSSL 3 without its legacy provider and by
        /// Android. Use only for consumers that cannot read <see cref="Aes256"/>, such as
        /// Windows Server 2016 and earlier.
        /// </summary>
        Legacy = 1,
    }
}
