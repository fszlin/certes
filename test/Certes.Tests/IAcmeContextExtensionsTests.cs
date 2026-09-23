using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Moq;
using Xunit;

namespace Certes
{
    public class IAcmeContextExtensionsTests
    {
        [Fact]
        public async Task CanGetTos()
        {
            var tosUri = new Uri("http://acme.d/tos");
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(
                new Directory(null, null, null, null, null, new DirectoryMeta(tosUri, null, null, null), null));
            Assert.Equal(tosUri, await ctxMock.Object.TermsOfService());

            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(
                new Directory(null, null, null, null, null, new DirectoryMeta(null, null, null, null), null));
            Assert.Null(await ctxMock.Object.TermsOfService());

            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(
                new Directory(null, null, null, null, null, null, null));
            Assert.Null(await ctxMock.Object.TermsOfService());
        }
    }
}
