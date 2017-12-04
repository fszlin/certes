using Xunit;

namespace Certes.Jws
{
    public class RsaJsonWebKeyTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var key = new RsaJsonWebKey();
            key.VerifyGetterSetter(a => a.Exponent, "certes");
            key.VerifyGetterSetter(a => a.KeyType, "rsa");
            key.VerifyGetterSetter(a => a.Modulus, "13");
        }
    }
}
