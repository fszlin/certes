using Certes.Acme;
using Xunit;

namespace Certes.Cli
{
    public class AcmeContextFactoryTests
    {
        [Fact]
        public void CanCreateContext()
        {
            var factory = new AcmeContextFactory();

            var ctx = factory.Create(
                WellKnownServers.LetsEncryptV2, KeyFactory.NewKey(KeyAlgorithm.ES256));
            Assert.NotNull(ctx);

            ctx = factory.Create(
                WellKnownServers.LetsEncryptV2, null);
            Assert.NotNull(ctx);
        }
    }
}
