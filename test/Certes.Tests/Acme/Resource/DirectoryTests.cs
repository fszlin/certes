using Certes.Acme.Resource;
using System;
using Xunit;

namespace Certes.Tests.Acme.Resource
{
    public class DirectoryTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var model = new Directory();
            model.VerifyGetterSetter(a => a.NewNonce, new Uri("http://NewNonce.is.working"));
            model.VerifyGetterSetter(a => a.NewCert, new Uri("http://NewCert.is.working"));
            model.VerifyGetterSetter(a => a.NewAuthz, new Uri("http://NewAuthz.is.working"));
            model.VerifyGetterSetter(a => a.RevokeCert, new Uri("http://RevokeCert.is.working"));
            model.VerifyGetterSetter(a => a.KeyChange, new Uri("http://KeyChange.is.working"));
            model.VerifyGetterSetter(a => a.NewReg, new Uri("http://NewReg.is.working"));
            model.VerifyGetterSetter(a => a.NewAccount, new Uri("http://NewAccount.is.working"));
            model.VerifyGetterSetter(a => a.NewOrder, new Uri("http://NewOrder.is.working"));
            model.VerifyGetterSetter(a => a.Meta, new DirectoryMeta
            {
                Website = new Uri("http://certes.is.working")
            });
        }
    }
}
