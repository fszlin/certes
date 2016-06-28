using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using System;

namespace Certes.Pkcs
{
    public enum SignatureAlgorithm
    {
        Sha256WithRsaEncryption
    }

    public static class SignatureAlgorithmExtensions
    {
        public static string ToJwsAlgorithm(this SignatureAlgorithm algo)
        {
            if (algo == SignatureAlgorithm.Sha256WithRsaEncryption)
            {
                return "RS256";
            }

            throw new ArgumentException(nameof(algo));
        }

        internal static AsymmetricCipherKeyPair Create(this SignatureAlgorithm algo)
        {
            var generator = new RsaKeyPairGenerator();
            var generatorParams = new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), new SecureRandom(), 2048, 128);
            generator.Init(generatorParams);
            var keyPair = generator.GenerateKeyPair();
            return keyPair;
        }

        internal static string ToPkcsObjectId(this SignatureAlgorithm algo)
        {
            switch (algo)
            {
                case SignatureAlgorithm.Sha256WithRsaEncryption:
                    return PkcsObjectIdentifiers.Sha256WithRsaEncryption.Id;
            }

            return null;
        }
    }
}
