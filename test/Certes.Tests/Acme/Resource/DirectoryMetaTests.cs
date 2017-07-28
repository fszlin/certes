using Certes.Acme.Resource;
using System;
using Xunit;

namespace Certes.Tests.Acme.Resource
{
    public class DirectoryMetaTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var model = new DirectoryMeta();
            model.VerifyGetterSetter(a => a.TermsOfService, new Uri("http://TermsOfService.is.working"));
            model.VerifyGetterSetter(a => a.Website, new Uri("http://Website.is.working"));
            model.VerifyGetterSetter(a => a.CaaIdentities, new[] { "caa", "is", "working" });
        }
    }
}
