using System.IO;
using System.Threading.Tasks;
using Certes.Jws;
using Certes.Pkcs;

namespace Certes.Crypto
{
    /// <summary>
    /// Represents key parameters used for signing.
    /// </summary>
    /// <remarks>
    /// May add netcore implementations once the API is ready - https://github.com/dotnet/designs/issues/11.
    /// </remarks>
    internal interface ISignatureKey
    {
        SignatureAlgorithm Algorithm { get;}
        JsonWebKey JsonWebKey { get; }
        Task Save(Stream data);
    }
}
