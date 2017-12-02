using System;
using Xunit;

namespace Certes.Pkcs
{
    public class SignatureAlgorithmTests
    {
        [Fact]
        public void ToJwsAlgorithmInvalid()
        {
            Assert.Throws<ArgumentException>(() =>
                ((SignatureAlgorithm)1000).ToJwsAlgorithm());
        }

        [Fact]
        public void ToPkcsObjectIdInvalid()
        {
            Assert.Null(
                ((SignatureAlgorithm)1000).ToPkcsObjectId());
        }
    }
}
