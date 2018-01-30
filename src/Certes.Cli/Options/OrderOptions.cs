using System;
using System.Collections.Generic;

namespace Certes.Cli.Options
{
    internal class OrderOptions : OptionsV2Base
    {
        public OrderAction Action;
        public IReadOnlyList<string> Values;
        public AuthorizationType Validate;
        public Uri Location;
        public string CertKeyPath;
        public string DistinguishName;
    }
}
