using System;
using System.Threading.Tasks;

namespace Certes.Acme
{
    internal class EntityContext<T>
    {
        public IAcmeContext Context { get; }
        public Uri Location { get; }

        public EntityContext(
            IAcmeContext context,
            Uri location)
        {
            Context = context;
            Location = location;
        }

        public virtual async Task<T> Resource()
        {
            var resp = await Context.HttpClient.Get<T>(Location);
            return resp.Resource;
        }
    }

}
