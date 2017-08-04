using Certes.Acme.Resource;
using System;
using Xunit;

namespace Certes.Tests.Acme.Resource
{
    public class AuthorizationTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var authz = new Authorization();
            authz.VerifyGetterSetter(a => a.Status, AuthorizationStatus.Processing);
            authz.VerifyGetterSetter(a => a.Challenges, new[] { new AuthorizationIdentifierChallenge() });
            authz.VerifyGetterSetter(a => a.Expires, DateTimeOffset.Now);
            authz.VerifyGetterSetter(a => a.Identifier, new AuthorizationIdentifier());
            authz.VerifyGetterSetter(a => a.Scope, new Uri("http://certes.is.working"));
        }
    }
}
