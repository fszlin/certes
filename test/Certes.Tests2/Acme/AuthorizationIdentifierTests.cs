using Xunit;

namespace Certes.Acme
{
    public class AuthorizationIdentifierTests
    {
        [Fact]
        public void CanCompareIdentifier()
        {
            var i1 = new AuthorizationIdentifier
            {
                Type = "dns",
                Value = "www.example.com"
            };

            Assert.False(i1.Equals((object)null));
            Assert.False(i1.Equals((AuthorizationIdentifier)null));

            var i2 = new AuthorizationIdentifier
            {
                Type = "dns",
                Value = "www.other.com"
            };

            Assert.False(i1.Equals((object)i2));
            Assert.False(i1.Equals(i2));

            var i3 = new AuthorizationIdentifier
            {
                Type = "other",
                Value = "www.example.com"
            };

            Assert.False(i1.Equals((object)i3));
            Assert.False(i1.Equals(i3));

            var i4 = new AuthorizationIdentifier
            {
                Value = "www.example.com"
            };

            Assert.NotEqual(i4.GetHashCode(), i1.GetHashCode());
            Assert.False(i4.Equals(i1));

            var i5 = new AuthorizationIdentifier
            {
                Type = "other",
            };

            Assert.NotEqual(i5.GetHashCode(), i1.GetHashCode());
            Assert.False(i5.Equals(i1));

            var i6 = new AuthorizationIdentifier
            {
            };

            Assert.NotEqual(i6.GetHashCode(), i1.GetHashCode());
            Assert.False(i6.Equals(i1));
        }

        [Fact]
        public void CanHashIdentifier()
        {
            var i1 = new AuthorizationIdentifier
            {
                Type = "dns",
                Value = "www.example.com"
            };

            var i2 = new AuthorizationIdentifier
            {
                Type = "dns",
                Value = "www.example.com"
            };

            var i3 = new AuthorizationIdentifier
            {
                Type = "other",
                Value = "www.example.com"
            };

            Assert.Equal(i1.GetHashCode(), i2.GetHashCode());
            Assert.NotEqual(i1.GetHashCode(), i3.GetHashCode());
        }
    }
}
