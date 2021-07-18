using System;
using Certes.Acme;
using Certes.Acme.Resource;
using Xunit;

namespace Certes.Tests.Acme.Resource
{
    public class ChallengeTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var entity = new Challenge();
            entity.VerifyGetterSetter(e => e.Error, new AcmeError());
            entity.VerifyGetterSetter(e => e.Status, ChallengeStatus.Invalid);
            entity.VerifyGetterSetter(e => e.Token, "certes");
            entity.VerifyGetterSetter(e => e.Type, "http-01");
            entity.VerifyGetterSetter(e => e.Url, new Uri("http://www.example.com"));
            entity.VerifyGetterSetter(e => e.Validated, DateTimeOffset.Now);
        }
    }
}
