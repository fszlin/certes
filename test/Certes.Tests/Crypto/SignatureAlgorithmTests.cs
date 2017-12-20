using System;
using Certes.Pkcs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Xunit;

namespace Certes.Crypto
{
    public class SignatureAlgorithmTests
    {
        [Theory]
        [InlineData(SignatureAlgorithm.RS256)]
        [InlineData(SignatureAlgorithm.ES256)]
        [InlineData(SignatureAlgorithm.ES384)]
        [InlineData(SignatureAlgorithm.ES512)]
        public void InvalidKeyData(SignatureAlgorithm alogType)
        {
            var provider = new SignatureAlgorithmProvider();
            var algo = provider.Get(alogType);
            var dsaSpec = new DsaParameters(
                new BigInteger("7434410770759874867539421675728577177024889699586189000788950934679315164676852047058354758883833299702695428196962057871264685291775577130504050839126673"),
                new BigInteger("1138656671590261728308283492178581223478058193247"),
                new BigInteger("4182906737723181805517018315469082619513954319976782448649747742951189003482834321192692620856488639629011570381138542789803819092529658402611668375788410"));
            var dsaKpg = GeneratorUtilities.GetKeyPairGenerator("DSA");
            dsaKpg.Init(new DsaKeyGenerationParameters(new SecureRandom(), dsaSpec));
            var keyPair = dsaKpg.GenerateKeyPair();

            using (var data = new System.IO.MemoryStream(
                PrivateKeyInfoFactory.CreatePrivateKeyInfo((keyPair.Private)).GetDerEncoded()))
            {
                Assert.Throws<NotSupportedException>(() => algo.ReadKey(data));
            }
        }
    }
}
