using System;
using System.IO;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Jws;
using Certes.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;

namespace Certes
{
    public static partial class Helper
    {
        private static string accountKeyV1;

        public static Task<AccountKey> LoadkeyV1()
        {
            return Task.FromResult(new AccountKey(new KeyInfo { PrivateKeyInfo = Convert.FromBase64String(GetTestKeyV1()) }));
        }

        public static string GetTestKeyV1()
        {
            if (accountKeyV1 != null)
            {
                return accountKeyV1;
            }

            var pem = GetTestKey(KeyAlgorithm.RS256);
            using (var reader = new StringReader(pem))
            {
                var pemReader = new PemReader(reader);
                var pemKey = (AsymmetricCipherKeyPair)pemReader.ReadObject();
                var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(pemKey.Private);
                return accountKeyV1 = Convert.ToBase64String(privateKey.GetDerEncoded());
            }
        }

    }
}
