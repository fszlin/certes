using Xunit;

namespace Certes.Pkcs
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
