using System;
using Xunit;

namespace Certes.Acme
{
    public class RegistrationTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var model = new Registration();
            Assert.Equal(ResourceTypes.Registration, model.Resource);
            model.VerifyGetterSetter(a => a.Agreement, new Uri("http://certes.is.working"));
            model.VerifyGetterSetter(a => a.Authorizations, new Uri("http://certes.is.working"));
            model.VerifyGetterSetter(a => a.Certificates, new Uri("http://certes.is.working"));
            model.VerifyGetterSetter(a => a.Contact, new[] { "mailto:hello@example.com" });
            model.VerifyGetterSetter(a => a.Delete, true);
            model.VerifyGetterSetter(a => a.Resource, ResourceTypes.NewRegistration);
        }
    }
}
