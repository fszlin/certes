using Certes.Pkcs;
using Newtonsoft.Json;
using System.Text;

namespace Certes.Jws
{
    public interface IAccountKey
    {
        SignatureAlgorithm Algorithm { get; }
        byte[] SignData(byte[] data);
        byte[] ComputeHash(byte[] data);
        object Jwk { get; }
        KeyInfo Export();
    }

    public static class AccountKeyExtensions
    {
        private static readonly JsonSerializerSettings thumbprintSettings = new JsonSerializerSettings();

        public static byte[] GenerateThumbprint(this IAccountKey key)
        {
            var jwk = key.Jwk;
            var json = JsonConvert.SerializeObject(jwk, Formatting.None, thumbprintSettings);
            var bytes = Encoding.UTF8.GetBytes(json);
            var hashed = key.ComputeHash(bytes);

            return hashed;
        }
        
    }
}
