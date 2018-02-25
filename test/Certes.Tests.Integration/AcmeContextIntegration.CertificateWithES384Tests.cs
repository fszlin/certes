using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Certes
{
    public partial class AcmeContextIntegration
    {
        public class CertificateWithES384Tests : AcmeContextIntegration
        {
            public CertificateWithES384Tests(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public Task CanGenerateCertificateWithEC384()
                => CanGenerateCertificateWithEC(KeyAlgorithm.ES384);
        }
    }
}
