using Xunit;

namespace Certes.Acme
{
    public class WellKnownServersTests
    {
        [Fact]
        public void CanGetValidUri()
        {
            Assert.Equal("https://acme-v02.api.letsencrypt.org/directory", WellKnownServers.LetsEncryptV2.OriginalString);
            Assert.Equal("https://acme-staging-v02.api.letsencrypt.org/directory", WellKnownServers.LetsEncryptStagingV2.OriginalString);
        }
    }
}
