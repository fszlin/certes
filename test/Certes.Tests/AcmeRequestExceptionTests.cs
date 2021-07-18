using System.IO;
using Xunit;
using Certes.Acme;

#if !NETCOREAPP1_0
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace Certes
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

#if NET452
        [Fact]
        public void CanSerialize()
        {
            var ex = new AcmeRequestException("certes");

            var serializer = new BinaryFormatter();

            using (var buffer = new MemoryStream())
            {
                serializer.Serialize(buffer, ex);

                buffer.Seek(0, SeekOrigin.Begin);
                var deserialized = (AcmeRequestException)serializer.Deserialize(buffer);

                Assert.Equal("certes", deserialized.Message);
            }
        }

        [Fact]
        public void CanSerializeWithErrorAndMessage()
        {
            var error = new AcmeError { Type = "t", Detail = "error" };
            var ex = new AcmeRequestException("certes", error);

            var serializer = new BinaryFormatter();

            using (var buffer = new MemoryStream())
            {
                serializer.Serialize(buffer, ex);

                buffer.Seek(0, SeekOrigin.Begin);
                var deserialized = (AcmeRequestException)serializer.Deserialize(buffer);

                Assert.Equal("certes\nt: error", deserialized.Message);
                Assert.NotNull(deserialized.Error.Detail);
                Assert.Equal("error", deserialized.Error.Detail);
            }
        }
#endif
    }
}
