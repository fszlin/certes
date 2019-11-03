using System.Globalization;
using Xunit;

namespace Certes.Properties
{
    public class StringsTests
    {
        [Fact]
        public void CanCreateInstance()
        {
            var res = new Strings();
        }

#if !NETCOREAPP1_0
        [Fact]
        public void CanGetSetCulture()
        {
            Strings.Culture = CultureInfo.GetCultureInfo("fr-CA");
            Assert.Equal(CultureInfo.GetCultureInfo("fr-CA"), Strings.Culture);
        }
#endif
    }
}
