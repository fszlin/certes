using System;
using Xunit;

namespace Certes.Acme
{
    public class AcmeContextTests
    {
        [Fact]
        public void CanGetOrderByLocation()
        {
            var loc = new Uri("http://d.com/order/1");
            var ctx = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
            var order = ctx.Order(loc);

            Assert.Equal(loc, order.Location);
        }

        [Fact]
        public void CanGetAuthzByLocation()
        {
            var loc = new Uri("http://d.com/authz/1");
            var ctx = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
            var authz = ctx.Authorization(loc);

            Assert.Equal(loc, authz.Location);
        }
    }
}
