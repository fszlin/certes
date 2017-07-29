using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Json;
using Certes.Jws;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Certes.Tests
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
