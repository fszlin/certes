using System.Text;
using Xunit;

namespace Certes.Jws
{
    public class JwsConvertTests
    {
        [Fact]
        public void CanConvertToBase64String()
        {
            foreach (var s in new[]
            {
                "a", "ab", "abc", "abcd"
            })
            {
                var data = Encoding.UTF8.GetBytes(s);
                var str = JwsConvert.ToBase64String(data);
                var reverted = JwsConvert.FromBase64String(str);
                Assert.Equal(data, reverted);
            }

            Assert.Throws<AcmeException>(() => JwsConvert.FromBase64String("/not a valid base 64 string/!"));
        }
    }
}
