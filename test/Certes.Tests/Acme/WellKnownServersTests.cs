using Xunit;

namespace Certes.Acme
{
    public class WellKnownServersTests
    {
        [Fact]
        public void CanGetValidUri()
        {
            Assert.Equal("https://acme-v01.api.letsencrypt.org/directory", WellKnownServers.LetsEncrypt.OriginalString);
            Assert.Equal("https://acme-staging.api.letsencrypt.org/directory", WellKnownServers.LetsEncryptStaging.OriginalString);
        }
    }
}
