using System.Globalization;
using System.Linq;
using System.Reflection;
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

        // Strings.Designer.cs is generated from Strings.resx at design time only; this fails
        // when a resource is removed from the resx without regenerating the designer file.
        [Fact]
        public void EveryDesignerMemberHasAResource()
        {
            var members = typeof(Strings)
                .GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(p => p.PropertyType == typeof(string))
                .ToArray();

            Assert.NotEmpty(members);
            var missing = members
                .Where(p => Strings.ResourceManager.GetString(p.Name, CultureInfo.InvariantCulture) == null)
                .Select(p => p.Name)
                .ToArray();
            Assert.Empty(missing);
        }

        [Fact]
        public void Ctor()
        {
            var instance = new Strings();
        }
    }
}
