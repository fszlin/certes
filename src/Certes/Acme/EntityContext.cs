using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;

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

        public virtual async Task<Order> Resource()
        {
            var resp = await Context.HttpClient.Get<Order>(Location);
            return resp.Resource;
        }
    }

}
