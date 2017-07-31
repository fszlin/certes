using System;
using Xunit;

namespace Certes.Acme
{
    public class AcmeDirectoryTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
#pragma warning disable 0618

            var model = new AcmeDirectory();
            var assigned = model.Meta = new AcmeDirectory.AcmeDirectoryMeta
            {
                Website = new Uri("https://certes.is.working")
            };

            Assert.NotNull(model.Meta);
            Assert.Equal(assigned.Website, model.Meta.Website);
            Assert.Null(model.Meta.CaaIdentities);
            Assert.Null(model.Meta.TermsOfService);

            model.VerifyGetterSetter(a => a.Meta, null);

#pragma warning restore 0618
        }
    }
}
