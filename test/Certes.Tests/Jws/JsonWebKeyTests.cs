using Xunit;

namespace Certes.Jws
{
    public class JsonWebKeyTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var key = new JsonWebKey();
            key.VerifyGetterSetter(a => a.Exponent, "certes");
            key.VerifyGetterSetter(a => a.KeyType, "rsa");
            key.VerifyGetterSetter(a => a.Modulus, "13");
        }
    }
}
