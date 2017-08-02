using Xunit;

namespace Certes.Jws
{
    public class JwsUnprotectedHeaderTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var account = new JwsUnprotectedHeader();
            account.VerifyGetterSetter(a => a.Algorithm, "certes");
            account.VerifyGetterSetter(a => a.JsonWebKey, new JsonWebKey());
        }
    }
}
