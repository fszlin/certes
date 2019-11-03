using System;
using Xunit;

namespace Certes.Acme.Resource
{
    public class DirectoryMetaTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var data = new
            {
                Website = new Uri("http://certes.is.working"),
                CaaIdentities = new[] { "caa1", "caa2" },
                ExternalAccountRequired = true,
                TermsOfService = new Uri("http://certes.is.working/tos"),
            };

            var model = new DirectoryMeta(
                data.TermsOfService,
                data.Website,
                data.CaaIdentities,
                data.ExternalAccountRequired);

            Assert.Equal(data.TermsOfService, model.TermsOfService);
            Assert.Equal(data.Website, model.Website);
            Assert.Equal(data.CaaIdentities, model.CaaIdentities);
            Assert.Equal(data.ExternalAccountRequired, model.ExternalAccountRequired);
        }
    }
}
