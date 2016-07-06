using System.Net;

namespace Certes.Acme
{
    public class AcmeError
    {
        public string Type { get; set; }
        public string Detail { get; set; }
        public HttpStatusCode Status { get; set; }
    }
}
