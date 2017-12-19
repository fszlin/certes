using Certes.Json;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text.RegularExpressions;
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

        [Fact]
        public void CanBeSerialized()
        {
            var settings = JsonUtil.CreateSettings();
            var srcJson = File.ReadAllText("./Data/account.json");
            var deserialized = JsonConvert.DeserializeObject<Account>(srcJson);
            var json = JsonConvert.SerializeObject(deserialized);

            Assert.Equal(AccountStatus.Valid, deserialized.Status);

            Assert.Equal(Regex.Replace(srcJson, @"\s", "").Length, Regex.Replace(json, @"\s", "").Length);
        }
    }
}
