using System;
using Xunit;

namespace Certes.Acme.Resource
{
    public class OrderTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var entity = new Order();
            entity.VerifyGetterSetter(a => a.Authorizations, new[] { new Uri("http://certes.is.working") });
            entity.VerifyGetterSetter(a => a.Certificate, new Uri("http://certes.is.working"));
            entity.VerifyGetterSetter(a => a.Csr, "certes is working");
            entity.VerifyGetterSetter(a => a.Error, new { detail = "everything is fine" });
            entity.VerifyGetterSetter(a => a.Expires, DateTimeOffset.Now);
            entity.VerifyGetterSetter(a => a.NotAfter, DateTimeOffset.Now.AddDays(1));
            entity.VerifyGetterSetter(a => a.NotBefore, DateTimeOffset.Now.AddDays(-1));
            entity.VerifyGetterSetter(a => a.Status, OrderStatus.Processing);
        }
    }
}
