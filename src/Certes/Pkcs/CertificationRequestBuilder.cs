using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using System;

namespace Certes.Pkcs
{
    public class CertificationRequestBuilder : CertificationRequestBuilderBase
    {
        private KeyInfo keyInfo;

        protected override SignatureAlgorithm Algorithm { get; }
        protected override AsymmetricCipherKeyPair KeyPair { get; }

        public CertificationRequestBuilder(KeyInfo keyInfo)
        {
            this.keyInfo = keyInfo;
        }

        public CertificationRequestBuilder()
        {
            if (keyInfo == null)
            {
                this.KeyPair = SignatureAlgorithm.Sha256WithRsaEncryption.Create();
                this.Algorithm = SignatureAlgorithm.Sha256WithRsaEncryption;
            }
            else
            {
                this.KeyPair = keyInfo.CreateKeyPair();
                if (this.KeyPair.Private is RsaPrivateCrtKeyParameters)
                {
                    this.Algorithm = SignatureAlgorithm.Sha256WithRsaEncryption;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }

}
