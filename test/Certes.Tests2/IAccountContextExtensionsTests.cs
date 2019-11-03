using System.Threading.Tasks;
using Certes.Acme;
using Moq;
using Xunit;

namespace Certes
{
    public class IAccountContextExtensionsTests
    {
        [Fact]
        public async Task CanDeactivateAccount()
        {
            var ctx = new Mock<IAccountContext>();

            var tsk = Task.FromResult(ctx.Object);
            await tsk.Deactivate();
            ctx.Verify(m => m.Deactivate(), Times.Once);
        }
    }
}
