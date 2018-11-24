using Certes.Jws;
using Newtonsoft.Json;
using Xunit;

namespace Certes.Crypto
{
    public class SignatureKeyTests
    {
        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        private void CanExportKey(KeyAlgorithm signatureAlgorithm)
        {
            var provider = new KeyAlgorithmProvider();
            var algo = provider.Get(signatureAlgorithm);
            var key = algo.GenerateKey();
            Assert.NotNull(key);

            var der = key.ToDer();
            var exported = provider.GetKey(der);

            Assert.Equal(
                JsonConvert.SerializeObject(key.JsonWebKey),
                JsonConvert.SerializeObject(exported.JsonWebKey));

            var pem = key.ToPem();
            exported = provider.GetKey(pem);

            Assert.Equal(
                JsonConvert.SerializeObject(key.JsonWebKey),
                JsonConvert.SerializeObject(exported.JsonWebKey));
        }

        [Theory]
        [InlineData(Keys.ES256Key)]
        [InlineData(Keys.ES256Key_Alt1)]
        [InlineData(Keys.ES384Key)]
        [InlineData(Keys.ES512Key)]
        private void CanEncodeJsonWebKey(string key)
        {
            var k = KeyFactory.FromPem(key);
            var ecKey = (EcJsonWebKey)k.JsonWebKey;

            Assert.Equal("EC", ecKey.KeyType);
            Assert.Equal(ecKey.X.Length, ecKey.X.Length);
        }

        [Fact]
        private void CanPadECCoordBytes()
        {
            var k = KeyFactory.FromPem(Keys.ES256Key_Alt1);
            var ecKey = (EcJsonWebKey)k.JsonWebKey;

            Assert.Equal("AJz0yAAXAwEmOhTRkjXxwgedbWO6gobYM3lWszrS68E", ecKey.X);
            Assert.Equal("vEEs4V0egJkNyM2Q4pp001zu14VcpQ0_Ei8xOOPxKZs", ecKey.Y);

            k = KeyFactory.FromPem(Keys.ES256Key);
            ecKey = (EcJsonWebKey)k.JsonWebKey;

            Assert.Equal("dHVy6M_8l7UibLdFPlhnbdNv-LROnx6_FcdyFArBd_s", ecKey.X);
            Assert.Equal("2xBzsnlAASQN0jQYuxdWybSzEQtsxoT-z7XGIDp0k_c", ecKey.Y);
        }
    }
}
