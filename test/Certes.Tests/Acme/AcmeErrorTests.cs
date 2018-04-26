using System.Net;
using Certes.Acme.Resource;
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
            model.VerifyGetterSetter(a => a.Subproblems, new AcmeError[1]);
            model.VerifyGetterSetter(a => a.Identifier, new Identifier { Type = IdentifierType.Dns, Value = "www.abc.com" });
        }
    }
}
