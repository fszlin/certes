using System.IO;

namespace Certes.Crypto
{
    internal interface ISignatureAlgorithm
    {
        ISigner CreateSigner(ISignatureKey keyPair);
        ISignatureKey GenerateKey();
        ISignatureKey ReadKey(Stream data);
    }
}
