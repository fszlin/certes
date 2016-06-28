using System.Net;

namespace Certes.Acme
{

    public class AcmeRespone<T> : AcmeResult<T>
    {
        public string ReplayNonce { get; set; }
        public HttpStatusCode HttpStatus { get; set; }
        public AcmeError Error { get; set; }
    }
}
