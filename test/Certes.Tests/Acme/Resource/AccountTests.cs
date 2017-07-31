using System;
using Xunit;

namespace Certes.Acme.Resource
{
    public class AccountTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            var account = new Account();
            account.VerifyGetterSetter(a => a.Status, AccountStatus.Valid);
            account.VerifyGetterSetter(a => a.Contact, new string[] { "mailto:hello@example.com" });
            account.VerifyGetterSetter(a => a.Orders, new Uri("http://certes.is.working"));
            account.VerifyGetterSetter(a => a.TermsOfServiceAgreed, true);
        }
    }
}
