using Xunit;

namespace Certes.Jws
{
    public class AccountKeyTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var key = new AccountKey();
#pragma warning disable 0612
            Assert.Equal(key.Jwk, key.JsonWebKey);
#pragma warning restore 0612
        }
    }
}
