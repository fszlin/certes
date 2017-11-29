namespace Certes.Crypto
{
    internal interface ISigner
    {
        /// <summary>
        /// Computes the hash for given data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The hash.</returns>
        byte[] ComputeHash(byte[] data);

        /// <summary>
        /// Signs the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The signature.</returns>
        byte[] SignData(byte[] data);
    }
}
