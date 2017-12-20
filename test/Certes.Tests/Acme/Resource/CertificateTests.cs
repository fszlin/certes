using System;
using System.Collections.Generic;
using Xunit;

namespace Certes.Acme.Resource
{
    public class CertificateTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var obj = new Certificate();
            obj.VerifyGetterSetter(a => a.NotBefore, DateTimeOffset.Now.AddDays(-10));
            obj.VerifyGetterSetter(a => a.NotAfter, DateTimeOffset.Now.AddDays(100));
            obj.VerifyGetterSetter(a => a.Identifiers, new List<AuthorizationIdentifier>());
        }
    }
}
