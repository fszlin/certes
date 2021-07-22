using System;
using Certes.Pkcs;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Xunit;

namespace Certes.Crypto
{
    public class KeyAlgorithmProviderTests
    {
        [Theory]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        private void CanCreateEllipticCurveAlgo(KeyAlgorithm algorithm)
        {
            var provider = new KeyAlgorithmProvider();
            var algo = provider.Get(algorithm) as EllipticCurveAlgorithm;

            Assert.NotNull(algo);

            var key = algo.GenerateKey();
            Assert.NotNull(key);

            Assert.NotNull(key.JsonWebKey);
        }

        [Fact]
        public void CtorWithInvalidAlgo()
        {
            var provider = new KeyAlgorithmProvider();
            Assert.Throws<ArgumentException>(() =>
                provider.Get((KeyAlgorithm)100));
        }

        [Theory]
        [InlineData(KeyAlgorithm.RS256, null)]
        [InlineData(KeyAlgorithm.RS256, 2048)]
        [InlineData(KeyAlgorithm.RS256, 3072)]
        [InlineData(KeyAlgorithm.RS256, 4096)]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        public void CanGetKey(KeyAlgorithm algorithm, int? keySize = null)
        {
            var provider = new KeyAlgorithmProvider();
            var algo = provider.Get(algorithm);
            var key = (AsymmetricCipherKey)algo.GenerateKey(keySize);

            var keyData = key.ToDer();

            var restored = provider.GetKey(keyData);

            var restoredData = restored.ToDer();
            Assert.Equal(keyData, restoredData);
        }

        [Fact]
        public void InvalidCurve()
        {
            var provider = new KeyAlgorithmProvider();
            var generator = GeneratorUtilities.GetKeyPairGenerator("ECDSA");
            var generatorParams = new ECKeyGenerationParameters(
                CustomNamedCurves.GetOid("secp160r1"), new SecureRandom());
            generator.Init(generatorParams);
            var keyPair = generator.GenerateKeyPair();

            var der = PrivateKeyInfoFactory.CreatePrivateKeyInfo((keyPair.Private)).GetDerEncoded();
            Assert.Throws<NotSupportedException>(() => provider.GetKey(der));
        }

        [Fact]
        public void InvalidKeyData()
        {
            var provider = new KeyAlgorithmProvider();
            var dsaSpec = new DsaParameters(
                new BigInteger("7434410770759874867539421675728577177024889699586189000788950934679315164676852047058354758883833299702695428196962057871264685291775577130504050839126673"),
                new BigInteger("1138656671590261728308283492178581223478058193247"),
                new BigInteger("4182906737723181805517018315469082619513954319976782448649747742951189003482834321192692620856488639629011570381138542789803819092529658402611668375788410"));
            var dsaKpg = GeneratorUtilities.GetKeyPairGenerator("DSA");
            dsaKpg.Init(new DsaKeyGenerationParameters(new SecureRandom(), dsaSpec));
            var keyPair = dsaKpg.GenerateKeyPair();

            var der = PrivateKeyInfoFactory.CreatePrivateKeyInfo((keyPair.Private)).GetDerEncoded();
            Assert.Throws<NotSupportedException>(() => provider.GetKey(der));
        }
    }
}
