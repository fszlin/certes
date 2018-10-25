using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Certes
{
    public partial class AcmeContextIntegration
    {
        public class CertificateWithES256Tests : AcmeContextIntegration
        {
            public CertificateWithES256Tests(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public Task CanGenerateCertificateWithEC256()
                => CanGenerateCertificateWithEC(KeyAlgorithm.ES256);
        }
    }
}
