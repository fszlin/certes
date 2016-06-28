using System;
using System.Collections.Generic;

namespace Certes.Acme
{
    public class Authorization : EntityBase
    {
        public Authorization()
        {
            this.Resource = ResourceTypes.Authorization;
        }

        public AuthorizationIdentifier Identifier { get; set; }

        public string Status { get; set; }

        public DateTimeOffset Expires { get; set; }

        public IList<Challenge> Challenges { get; set; }

        public IList<IList<int>> Combinations { get; set; }
    }
}
