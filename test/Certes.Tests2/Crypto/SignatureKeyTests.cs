using Certes.Json;
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

        [Fact]
        private void EnsurePropertySerializationOrder()
        {
            /// https://tools.ietf.org/html/rfc7638#page-8
            /// The lexographical (alphabetical) order of the serialized key is important for JWK thumprint validation
            /// without a specified order this order can vary across platforms during property reflection.

            var k = KeyFactory.FromPem(Keys.ES256Key_Alt1);
            var ecKey = (EcJsonWebKey)k.JsonWebKey;
            var ecKeyJson = JsonConvert.SerializeObject(ecKey, Formatting.None, JsonUtil.CreateSettings());

            Assert.Equal(
                "{\"crv\":\"P-256\",\"kty\":\"EC\",\"x\":\"AJz0yAAXAwEmOhTRkjXxwgedbWO6gobYM3lWszrS68E\",\"y\":\"vEEs4V0egJkNyM2Q4pp001zu14VcpQ0_Ei8xOOPxKZs\"}"
                , ecKeyJson
                );

            k = KeyFactory.FromPem(Keys.RS256Key);
            var rsaKey = (RsaJsonWebKey)k.JsonWebKey;
            var rsaKeyJson = JsonConvert.SerializeObject(rsaKey, Formatting.None, JsonUtil.CreateSettings());

            Assert.Equal(
                "{\"e\":\"AQAB\",\"kty\":\"RSA\",\"n\":\"maeT6EsXTVHAdwuq3IlAl9uljXE5CnkRpr6uSw_Fk9nQshfZqKFdeZHkSBvIaLirE2ZidMEYy-rpS1O2j-viTG5U6bUSWo8aoeKoXwYfwbXNboEA-P4HgGCjD22XaXAkBHdhgyZ0UBX2z-jCx1smd7nucsi4h4RhC_2cEB1x_mE6XS5VlpvG91Hbcgml4cl0NZrWPtJ4DhFdPNUtQ8q3AYXkOr_OSFZgRKjesRaqfnSdJNABqlO_jEzAx0fgJfPZe1WlRWOfGRVBVopZ4_N5HpR_9lsNDzCZyidFsHwzvpkP6R6HbS8CMrNWgtkTbnz27EVqIhkYdiPVIN2Xkwj0BQ\"}"
                ,
                rsaKeyJson
                );
        }
    }
}
