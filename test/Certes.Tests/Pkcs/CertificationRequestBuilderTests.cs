using Certes.Pkcs;
using Xunit;

namespace Certes.Tests.Pkcs
{
    public class CertificationRequestBuilderTests
    {
        [Fact]
        public void CanCreateCsrWithKey()
        {
            var key = Helper.Loadkey();
            new CertificationRequestBuilder(key.Export());
        }
    }
}
