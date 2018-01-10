using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Crypto;
using Certes.Pkcs;

using Directory = Certes.Acme.Resource.Directory;

namespace Certes
{
    public static partial class Helper
    {
        internal static readonly Directory MockDirectoryV2 = new Directory(
            new Uri("http://acme.d/newNonce"),
            new Uri("http://acme.d/newAccount"),
            new Uri("http://acme.d/newOrder"),
            new Uri("http://acme.d/revokeCert"),
            new Uri("http://acme.d/keyChange"),
            new DirectoryMeta(new Uri("http://acme.d/tos"), null, null, false));

        private static Uri stagingServerV2;

        internal static ISignatureKey GetAccountKey(SignatureAlgorithm algo = SignatureAlgorithm.ES256)
        {
            return DSA.FromPem(algo.GetTestKey());
        }

        public static async Task<Uri> GetAvailableStagingServerV2()
        {
            if (stagingServerV2 != null)
            {
                return stagingServerV2;
            }

            var servers = new[] {
                new Uri("http://localhost:4001/directory"),
                new Uri("http://boulder-certes-ci.dymetis.com:4001/directory"),
                WellKnownServers.LetsEncryptStagingV2,
            };

            using (var http = new HttpClient())
            {
                foreach (var uri in servers)
                {
                    try
                    {
                        await http.GetStringAsync(uri);

                        foreach (var algo in Enum.GetValues(typeof(SignatureAlgorithm)).OfType<SignatureAlgorithm>())
                        {
                            try
                            {
                                var ctx = new AcmeContext(uri, Helper.GetAccountKey(algo));
                                await ctx.NewAccount(new[] { "mailto:fszlin@example.com" }, true);
                            }
                            catch
                            {
                            }
                        }

                        return stagingServerV2 = uri;
                    }
                    catch
                    {
                    }
                }
            }

            throw new Exception("No staging server available.");
        }
    }
}
