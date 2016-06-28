using Certes.Pkcs;

namespace Certes.Acme
{
    public class KeyedAcmeResult<T> : AcmeResult<T>
    {
        public KeyInfo Key { get; set; }
    }
}
