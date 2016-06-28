using System;
using System.Collections.Generic;
using System.Linq;

namespace Certes.Acme
{

    public class AcmeResult<T>
    {
        public T Data { get; set; }
        public byte[] Raw { get; set; }
        public Uri Location { get; set; }
        public IList<RelLink> Links { get; set; }
        
        public string ContentType { get; set; }
        
        public string Json { get; set; }
    }

    public static class AcmeResultExtensions
    {
        public static Uri GetTermsOfServiceUri<T>(this AcmeResult<T> result)
        {
            return result.Links.FirstOrDefault(l => l.Rel == "terms-of-service")?.Uri;
        }
    }
}
