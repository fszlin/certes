using System;

namespace Certes.Acme
{
    public class Certificate : EntityBase
    {
        public Certificate()
        {
            Resource = ResourceTypes.Certificate;
        }

        public string Csr { get; set; }
        public DateTimeOffset? NotBefore { get; set; }
        public DateTimeOffset? NotAfter { get; set; }
    }
}
