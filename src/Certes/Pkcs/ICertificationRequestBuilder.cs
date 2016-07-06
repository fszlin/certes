namespace Certes.Pkcs
{
    public interface ICertificationRequestBuilder
    {
        void AddName(string symbol, string value);
        byte[] Generate();
        KeyInfo Export();
    }
}
