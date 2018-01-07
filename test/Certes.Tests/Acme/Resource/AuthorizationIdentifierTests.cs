using Certes.Acme.Resource;
using System;
using Xunit;

namespace Certes.Tests.Acme.Resource
{
    public class AuthorizationIdentifierTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var authorizationIdentifier = new Identifier();
            authorizationIdentifier.VerifyGetterSetter(a => a.Type, IdentifierType.Dns);
            authorizationIdentifier.VerifyGetterSetter(a => a.Value, "certes is working");
        }
    }
}
