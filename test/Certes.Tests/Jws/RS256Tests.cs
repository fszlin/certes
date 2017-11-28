using Xunit;

namespace Certes.Jws
{
    public class RS256Tests
    {
        [Fact]
        public void CanExportJwk()
        {
            var kp = RS256.CreateKeyPair();
            var jwa = new RS256(kp);
            var jwk = jwa.JsonWebKey;
            Assert.NotNull(jwk);
        }
    }
}
