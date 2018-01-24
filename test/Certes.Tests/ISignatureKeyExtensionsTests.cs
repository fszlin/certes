using System.Security.Cryptography;
using System.Text;
using Certes.Jws;
using Xunit;

namespace Certes
{
    public class ISignatureKeyExtensionsTests
    {
        [Fact]
        public void CanGenerateDnsRecordValue()
        {
            var key = KeyFactory.NewKey(KeyAlgorithm.ES256);
            using (var sha256 = SHA256.Create())
            {
                Assert.Equal(
                    JwsConvert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(key.KeyAuthorization("token")))),
                    key.DnsTxt("token"));
            }
        }
    }
}
