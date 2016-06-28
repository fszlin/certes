using System;
using System.Collections.Generic;

namespace Certes.Acme
{
    public class Challenge : EntityBase
    {
        public Challenge()
        {
            this.Resource = ResourceTypes.Challenge;
        }
        public string Type { get; set; }
        public Uri Uri { get; set; }
        public string Token { get; set; }
        public string Status { get; set; }
        public DateTimeOffset? Validated { get; set; }
        public string KeyAuthorization { get; set; }
        public IList<ChallengeValidation> ValidationRecord { get; set; }
    }

}
