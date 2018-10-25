using Xunit;

namespace Certes.Cli
{
    public class CertesCliExceptionTests
    {
        [Fact]
        public void CanCreateException()
        {
            var ex = new CertesCliException();
        }

        [Fact]
        public void CanCreateExceptionWithMessage()
        {
            var ex = new CertesCliException("certes");
            Assert.Equal("certes", ex.Message);
        }

        [Fact]
        public void CanCreateExceptionWithInnerException()
        {
            var inner = new AcmeException();
            var ex = new CertesCliException("certes", inner);
            Assert.Equal("certes", ex.Message);
            Assert.Equal(inner, ex.InnerException);
        }
    }
}
