using System;
using System.Collections.Generic;

namespace Certes.Acme
{
    public class ChallengeValidation
    {
        public Uri Url { get; set; }
        public string Hostname { get; set; }
        public IList<string> AddressesResolved { get; set; }
        public string AddressUsed { get; set; }
    }
}
