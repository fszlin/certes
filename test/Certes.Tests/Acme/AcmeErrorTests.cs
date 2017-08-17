using System.Net;
using Xunit;

namespace Certes.Acme
{
    public class AcmeErrorTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var model = new AcmeError();
            model.VerifyGetterSetter(a => a.Detail, "error details");
            model.VerifyGetterSetter(a => a.Status, HttpStatusCode.ExpectationFailed);
            model.VerifyGetterSetter(a => a.Type, "error type");
        }
    }
}
