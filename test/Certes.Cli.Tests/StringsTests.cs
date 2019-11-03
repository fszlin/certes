using System.Globalization;
using Xunit;

namespace Certes.Cli
{
    public class StringsTests
    {
        [Fact]
        public void CanSetCulture()
        {
            var fr = new CultureInfo("fr-CA");
            Strings.Culture = fr;

            Assert.Equal(fr, Strings.Culture);
        }

        [Fact]
        public void CanGetResManager()
        {
            Assert.NotNull(Strings.ResourceManager);
        }

        [Fact]
        public void Ctor()
        {
            var instance = new Strings();
        }
    }
}
