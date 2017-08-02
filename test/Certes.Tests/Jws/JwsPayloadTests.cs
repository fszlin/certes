using Xunit;

namespace Certes.Jws
{
    public class JwsPayloadTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var account = new JsonWebKey();
            account.VerifyGetterSetter(a => a.Exponent, "certes");
            account.VerifyGetterSetter(a => a.KeyType, "rsa");
            account.VerifyGetterSetter(a => a.Modulus, "13");
        }
    }
}
