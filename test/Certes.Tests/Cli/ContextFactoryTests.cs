using System;
using Xunit;

namespace Certes.Cli
{
    [Collection(nameof(ContextFactory))]
    public class ContextFactoryTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            Func<Uri, IKey, IAcmeContext> cb = (Uri loc, IKey key) => null;

            ContextFactory.Create = cb;
            Assert.Equal(cb, ContextFactory.Create);
        }
    }
}
