using Certes.Acme;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Acme
{
    public class Registration : EntityBase
    {
        public Registration()
        {
            this.Resource = ResourceTypes.Registration;
        }

        public IList<string> Contact { get; set; } = new List<string>();
        public Uri Agreement { get; set; }
        public Uri Authorizations { get; set; }
        public Uri Certificates { get; set; }
    }
}
