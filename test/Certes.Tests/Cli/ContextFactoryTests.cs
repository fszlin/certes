using System;
using Xunit;

namespace Certes.Cli
{
    public class ContextFactoryTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            Func<Uri, IKey, IAcmeContext> cb = (Uri loc, IKey key) => new Certes.AcmeContext(loc, key);

            ContextFactory.Create = cb;
            Assert.Equal(cb, ContextFactory.Create);
        }
    }
}
