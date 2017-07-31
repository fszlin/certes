using Certes.Jws;
using System.Linq;
using Xunit;

namespace Certes.Tests.Cli
{
    public class JwsConvertTests
    {
        [Fact]
        public void CanConvertToBase64String()
        {
            var data = Enumerable
                .Range(0, 1000)
                .Select(i => (byte)i)
                .ToArray();

            var str = JwsConvert.ToBase64String(data);

            var reverted = JwsConvert.FromBase64String(str);

            Assert.Equal(data, reverted);
        }
    }
}
