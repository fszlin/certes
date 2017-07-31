using Certes.Acme;
using System.Threading.Tasks;
using Xunit;

namespace Certes
{
    public class AcmeClientStagingTests
    {
        [Fact]
        public async Task CanDeleteRegistration()
        {
            using (var client = new AcmeClient(WellKnownServers.LetsEncryptStaging))
            {
                var reg = await client.NewRegistraton();
                await client.DeleteRegistration(reg);
            }
        }

    }
}
