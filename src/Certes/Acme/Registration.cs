using System;
using System.Collections.Generic;

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
