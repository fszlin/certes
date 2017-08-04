using Certes.Acme.Resource;
using System;
using Xunit;

namespace Certes.Tests.Acme.Resource
{
    public class AuthorizationIdentifierChallengeTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var entity = new AuthorizationIdentifierChallenge();
            entity.VerifyGetterSetter(e => e.Errors, new object[0]);
            entity.VerifyGetterSetter(e => e.Status, AuthorizationIdentifierChallengeStatus.Invalid);
            entity.VerifyGetterSetter(e => e.Token, "certes");
            entity.VerifyGetterSetter(e => e.Type, "http-01");
            entity.VerifyGetterSetter(e => e.Url, new Uri("http://www.example.com"));
            entity.VerifyGetterSetter(e => e.Validated, DateTimeOffset.Now);
        }
    }
}
