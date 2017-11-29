using Xunit;

namespace Certes.Jws
{
    public class ES256Tests
    {
        [Fact]
        public void CanExportJwk()
        {
            var kp = ES256.CreateKeyPair();
            var jwa = new ES256(kp);
            var jwk = jwa.JsonWebKey as EcJsonWebKey;

            Assert.NotNull(jwk);
            Assert.Equal("EC", jwk.KeyType);
            Assert.Equal("P-256", jwk.Curve);
        }
    }
}
