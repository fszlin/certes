using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Pkcs
{
    public interface ICertificationRequestBuilder
    {
        void AddName(string symbol, string value);
        byte[] Generate();
        KeyInfo Export();
    }
}
