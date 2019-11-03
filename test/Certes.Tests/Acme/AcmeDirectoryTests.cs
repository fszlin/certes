using System;
using Xunit;

namespace Certes.Acme
{
    public class AcmeDirectoryTests
    {
        // [Fact]
        public void CanGetSetProperties()
        {
            var model = new AcmeDirectory();
            model.VerifyGetterSetter(a => a.NewCert, new Uri("http://NewCert.is.working"));
            model.VerifyGetterSetter(a => a.NewAuthz, new Uri("http://NewAuthz.is.working"));
            model.VerifyGetterSetter(a => a.RevokeCert, new Uri("http://RevokeCert.is.working"));
            model.VerifyGetterSetter(a => a.KeyChange, new Uri("http://KeyChange.is.working"));
            model.VerifyGetterSetter(a => a.NewReg, new Uri("http://NewReg.is.working"));
            model.VerifyGetterSetter(a => a.Meta, new AcmeDirectory.AcmeDirectoryMeta
            {
                TermsOfService = new Uri("http://certes.is.working")
            });
        }
    }
}
