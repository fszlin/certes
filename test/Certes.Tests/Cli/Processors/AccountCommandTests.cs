#if NETCOREAPP1_0 || NETCOREAPP2_0

using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Options;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Certes.Cli.Processors
{
    public class AccountCommandTests
    {
        [Fact]
        public void CanParseCommnad()
        {
            var options = Parse("noop");
            Assert.Null(options);

            options = Parse("account");
            Assert.Equal(AccountAction.Info, options.Action);

            options = Parse("account new --email admin@example.com --agree-tos");
            Assert.Equal(AccountAction.New, options.Action);
            Assert.Equal("admin@example.com", options.Email);
            Assert.True(options.AgreeTos);

            options = Parse("account update --email admin@example.com --agree-tos");
            Assert.Equal(AccountAction.Update, options.Action);
            Assert.Equal("admin@example.com", options.Email);
            Assert.True(options.AgreeTos);

            options = Parse("account deactivate");
            Assert.Equal(AccountAction.Deactivate, options.Action);

            options = Parse("account change-key");
            Assert.Equal(AccountAction.ChangeKey, options.Action);

            options = Parse("account set --key ./account-key.pem");
            Assert.Equal(AccountAction.Set, options.Action);
            Assert.Equal("./account-key.pem", options.Path);

            Assert.Throws<ArgumentSyntaxException>(() => Parse("account new"));
            Assert.Throws<ArgumentSyntaxException>(() => Parse("account update"));
            Assert.Throws<ArgumentSyntaxException>(() => Parse("account set"));
        }

        [Fact]
        public async Task CanShowAccountInfo()
        {
            var account = new Account
            {
                TermsOfServiceAgreed = true,
                Contact = new[] { "mailto:admin@example.com" }
            };

            var acctMock = new Mock<IAccountContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.Setup(c => c.Account()).ReturnsAsync(acctMock.Object);
            acctMock.Setup(c => c.Resource()).ReturnsAsync(account);
            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var proc = new AccountCommand(new AccountOptions
            {
                Action = AccountAction.Info,
                Path = "./Data/key-es256.pem",
            });

            File.WriteAllText("./Data/key-es256.pem", Helper.GetTestKey(KeyAlgorithm.ES256));

            var ret = await proc.Process();

            Assert.Equal(JsonConvert.SerializeObject(account), JsonConvert.SerializeObject(ret));
        }

        [Fact]
        public async Task ShouldNotShowAccountInfoWhenNoKey()
        {
            var account = new Account
            {
                TermsOfServiceAgreed = true,
                Contact = new[] { "mailto:admin@example.com" }
            };

            var acctMock = new Mock<IAccountContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.Setup(c => c.Account()).ReturnsAsync(acctMock.Object);
            acctMock.Setup(c => c.Resource()).ReturnsAsync(account);
            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var proc = new AccountCommand(new AccountOptions
            {
                Action = AccountAction.Info,
                Path = "./nokey.pem"
            });

            await Assert.ThrowsAsync<Exception>(() => proc.Process());
        }

        private AccountOptions Parse(string cmd)
        {
            AccountOptions options = null;
            ArgumentSyntax.Parse(cmd.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                options = AccountCommand.TryParse(syntax);
            });

            return options;
        }
    }
}

#endif
