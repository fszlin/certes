using System.Text;
using Certes.Jws;
using Certes.Pkcs;
using Microsoft.Azure.Functions.Worker.Http;

namespace Certes.Func
{
    internal static class Helper
    {
        public static string Env(string name) =>
            Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process) ?? "";

        public static AccountKey GetTestKey(KeyAlgorithm algo)
        {
            var key =
                algo == KeyAlgorithm.ES256 ? Keys.ES256Key :
                algo == KeyAlgorithm.ES384 ? Keys.ES384Key :
                algo == KeyAlgorithm.ES512 ? Keys.ES512Key :
                Keys.RS256Key;

            using (var buffer = new MemoryStream(Encoding.UTF8.GetBytes(key)))
            {
                return new AccountKey(KeyInfo.From(buffer));
            }
        }

        public static AccountKey GetTestKey(HttpRequestData request)
        {
            var host = request.Url.Host;
            return
                host.IndexOf("-es256.", StringComparison.OrdinalIgnoreCase) >= 0 ? GetTestKey(KeyAlgorithm.ES256) :
                host.IndexOf("-es384.", StringComparison.OrdinalIgnoreCase) >= 0 ? GetTestKey(KeyAlgorithm.ES384) :
                host.IndexOf("-es512.", StringComparison.OrdinalIgnoreCase) >= 0 ? GetTestKey(KeyAlgorithm.ES512) :
                GetTestKey(KeyAlgorithm.RS256);
        }
    }
}
