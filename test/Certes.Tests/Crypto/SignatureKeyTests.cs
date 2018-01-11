using System.IO;
using Certes.Pkcs;
using Newtonsoft.Json;
using Xunit;

namespace Certes.Crypto
{
    public class SignatureKeyTests
    {
        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        private void CanExportKey(KeyAlgorithm signatureAlgorithm)
        {
            var provider = new KeyAlgorithmProvider();
            var algo = provider.Get(signatureAlgorithm);
            var key = algo.GenerateKey();
            Assert.NotNull(key);

            var der = key.ToDer();
            var exported = provider.GetKey(der);

            Assert.Equal(
                JsonConvert.SerializeObject(key.JsonWebKey),
                JsonConvert.SerializeObject(exported.JsonWebKey));
        }
    }
}
