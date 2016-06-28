using Certes.Acme;
using System.Collections.Generic;

namespace Certes.Cli
{
    public class AcmeContext
    {
        public AcmeAccount Account { get; set; }
        public IDictionary<string, IDictionary<string, AcmeResult<Authorization>>> Authorizations { get; set; }
        public IDictionary<string, AcmeCertificate> Certificates {get;set;}
    }
}
