namespace Certes.Crypto
{
    internal interface IKeyAlgorithm
    {
        ISigner CreateSigner(IKey keyPair);
        IKey GenerateKey(int? keySize = null);
    }
}
