using System.Linq;
using Xunit;

namespace Certes
{
    public class CsrInfoTests
    {
        [Fact]
        public void CanSetInfo()
        {
            var csr = new CsrInfo
            {
                CommonName = "CommonName",
                Locality = "Locality",
                CountryName = "CountryName",
                Organization = "Organization",
                OrganizationUnit = "OrganizationUnit",
                State = "State",
            };

            Assert.Equal("CommonName", csr.Fields.Single(f => f.name == "CN").value);
            Assert.Equal("Locality", csr.Fields.Single(f => f.name == "L").value);
            Assert.Equal("CountryName", csr.Fields.Single(f => f.name == "C").value);
            Assert.Equal("Organization", csr.Fields.Single(f => f.name == "O").value);
            Assert.Equal("OrganizationUnit", csr.Fields.Single(f => f.name == "OU").value);
            Assert.Equal("State", csr.Fields.Single(f => f.name == "ST").value);
        }
    }
}
