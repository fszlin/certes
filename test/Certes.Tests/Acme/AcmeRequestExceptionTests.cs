using System.IO;
using Xunit;

#if !NETCOREAPP1_0
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace Certes.Acme
{
    public class AcmeRequestExceptionTests
    {
        [Fact]
        public void CanCreateException()
        {
            var ex = new AcmeRequestException();
        }

        [Fact]
        public void CanCreateExceptionWithMessage()
        {
            var ex = new AcmeRequestException("certes");
            Assert.Equal("certes", ex.Message);
        }

        [Fact]
        public void CanCreateExceptionWithInnerException()
        {
            var inner = new AcmeException();
            var ex = new AcmeRequestException("certes", inner);
            Assert.Equal("certes", ex.Message);
            Assert.Equal(inner, ex.InnerException);
        }

#if !NETCOREAPP1_0
        [Fact]
        public void CanSerialize()
        {
            var error = new AcmeError { Detail = "error" };
            var ex = new AcmeRequestException("certes", error);
            
            var serializer = new BinaryFormatter();

            using (var buffer = new MemoryStream())
            {
                serializer.Serialize(buffer, ex);

                buffer.Seek(0, SeekOrigin.Begin);
                var deserialized = (AcmeRequestException)serializer.Deserialize(buffer);

                Assert.NotNull(deserialized.Error.Detail);
            }
        }
#endif
    }
}
