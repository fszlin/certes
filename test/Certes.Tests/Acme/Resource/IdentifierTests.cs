using Certes.Acme.Resource;
using Xunit;

namespace Certes.Tests.Acme.Resource
{
    public class IdentifierTests
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
