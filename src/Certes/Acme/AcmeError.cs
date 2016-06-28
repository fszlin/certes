using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Certes.Acme
{
    public class AcmeError
    {
        public string Type { get; set; }
        public string Detail { get; set; }
        public HttpStatusCode Status { get; set; }
    }
}
