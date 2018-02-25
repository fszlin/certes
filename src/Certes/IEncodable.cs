namespace Certes
{
    /// <summary>
    /// Supports exporting to PEM or DER format.
    /// </summary>
    public interface IEncodable
    {

        /// <summary>
        /// Exports to DER.
        /// </summary>
        /// <returns>DER encoded data.</returns>
        byte[] ToDer();

        /// <summary>
        /// Exports to PEM.
        /// </summary>
        /// <returns>PEM encoded data.</returns>
        string ToPem();
    }

}
