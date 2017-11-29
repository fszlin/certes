using System.IO;

namespace Certes.Jws
{
    internal interface IJsonWebAlgorithm
    {
        JsonWebKey JsonWebKey { get; }
        byte[] ComputeHash(byte[] data);
        byte[] SignData(byte[] data);
    }
}
