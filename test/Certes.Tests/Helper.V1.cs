using System;
using System.IO;
using System.Net.Http;
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
        private static Uri[] StagingServersV1 = new[]
        {
            new Uri("http://localhost:4000/directory"),
            new Uri("http://boulder-certes-ci.dymetis.com:4000/directory"),
            WellKnownServers.LetsEncryptStaging,
        };

        internal static AcmeDirectory MockDirectoryV1 = new AcmeDirectory
        {
            Meta = new AcmeDirectory.AcmeDirectoryMeta
            {
                TermsOfService = new Uri("http://example.com/tos.pdf")
            },
            NewAuthz = new Uri("http://example.com/new-authz"),
            NewCert = new Uri("http://example.com/new-cert"),
            NewReg = new Uri("http://example.com/new-reg"),
            RevokeCert = new Uri("http://example.com/revoke-cert")
        };


        private static Uri stagingServerV1;
        private static string accountKeyV1;

        internal static Task<AccountKey> LoadkeyV1()
        {
            return Task.FromResult(new AccountKey(new KeyInfo { PrivateKeyInfo = Convert.FromBase64String(GetTestKeyV1()) }));
        }

        internal static string GetTestKeyV1()
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

        internal static async Task<Uri> GetStagingServerV1()
        {
            if (stagingServerV1 != null)
            {
                return stagingServerV1;
            }

            var key = await LoadkeyV1();
            foreach (var uri in StagingServersV1)
            {
                var httpSucceed = false;
                try
                {
                    await http.Value.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                    httpSucceed = true;
                }
                catch
                {
                }

                if (httpSucceed)
                {
                    using (var client = new AcmeClient(uri))
                    {
                        client.Use(key.Export());

                        try
                        {
                            var account = await client.NewRegistraton();
                            account.Data.Agreement = account.GetTermsOfServiceUri();
                            await client.UpdateRegistration(account);
                        }
                        catch
                        {
                            // account already exists
                        }

                        return stagingServerV1 = uri;
                    }
                }
            }

            throw new Exception("Staging server unavailable.");
        }

    }
}
