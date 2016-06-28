using Certes.Acme;
using System;
using System.Collections.Generic;

namespace Certes.Cli.Options
{
    internal class AuthorizationOptions : OptionsBase
    {
        public string Type = AuthorizationIdentifierTypes.Dns;
        public IReadOnlyList<string> Values = Array.Empty<string>();
        public string Complete = "";
        public string KeyAuthentication = "";
        public string ValuesFile = "";
        public string Refresh = "";
    }
}
