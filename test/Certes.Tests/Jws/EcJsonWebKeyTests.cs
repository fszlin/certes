using Xunit;

namespace Certes.Jws
{
    public class EcJsonWebKeyTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var key = new EcJsonWebKey();
            key.VerifyGetterSetter(a => a.Curve, "P-256");
            key.VerifyGetterSetter(a => a.KeyType, "rsa");
            key.VerifyGetterSetter(a => a.X, "99");
            key.VerifyGetterSetter(a => a.Y, "199");
        }
    }
}
