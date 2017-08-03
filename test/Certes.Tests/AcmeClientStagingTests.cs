using Certes.Acme;
using Certes.Jws;
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

        [Fact]
        public async Task CanChangeKey()
        {
            using (var client = new AcmeClient(WellKnownServers.LetsEncryptStaging))
            {
                var reg = await client.NewRegistraton();

                var newKey = new AccountKey().Export();
                await client.ChangeKey(reg, newKey);
                await client.DeleteRegistration(reg);
            }
        }
    }
}
