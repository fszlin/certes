using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Options;
using Certes.Cli.Settings;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Certes.Cli.Processors
{
    [Collection(nameof(IntegrationTests))]
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
        public async Task CanNewAccount()
        {
            var keyPath = $"./Data/{nameof(CanNewAccount)}/key.pem";
            if (File.Exists(keyPath))
            {
                File.Delete(keyPath);
            }

            var account = new Account
            {
                TermsOfServiceAgreed = true,
                Contact = new[] { "mailto:admin@example.com" }
            };

            var acctMock = new Mock<IAccountContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.SetupGet(c => c.AccountKey).Returns(Helper.GetKeyV2());
            ctxMock.Setup(c => c.NewAccount(new[] { "mailto:admin@example.com" }, true))
                .ReturnsAsync(acctMock.Object);
            acctMock.Setup(c => c.Resource()).ReturnsAsync(account);
            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var options = new AccountOptions
            {
                Action = AccountAction.New,
                Email = "admin@example.com",
                AgreeTos = true,
                Path = keyPath,
            };

            var proc = new AccountCommand(options);

            var ret = await proc.Process();
            Assert.NotNull(ret);
            Assert.NotNull(UserSettings.GetAccountKey(options));

            // should not allow to overwrite the key file
            await Assert.ThrowsAsync<Exception>(() => proc.Process());
        }

        [Fact]
        public async Task CanShowAccountInfo()
        {
            var keyPath = $"./Data/{nameof(CanShowAccountInfo)}/key.pem";
            var account = new
            {
                uri = new Uri("http://acme.d/acct/1"),
                data = new Account
                {
                    TermsOfServiceAgreed = true,
                    Contact = new[] { "mailto:admin@example.com" }
                }
            };

            var acctMock = new Mock<IAccountContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.Setup(c => c.Account()).ReturnsAsync(acctMock.Object);
            acctMock.Setup(c => c.Resource()).ReturnsAsync(account.data);
            acctMock.SetupGet(c => c.Location).Returns(account.uri);
            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var proc = new AccountCommand(new AccountOptions
            {
                Action = AccountAction.Info,
                Path = keyPath,
            });

            Helper.SaveKey(keyPath);

            var ret = await proc.Process();
            Assert.Equal(JsonConvert.SerializeObject(account), JsonConvert.SerializeObject(ret));
        }

        [Fact]
        public async Task CanDeactivateAccount()
        {
            var keyPath = $"./Data/{nameof(CanDeactivateAccount)}/key.pem";
            var account = new
            {
                uri = new Uri("http://acme.d/acct/1"),
                data = new Account
                {
                    Status = AccountStatus.Deactivated,
                }
            };

            var acctMock = new Mock<IAccountContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.Setup(c => c.Account()).ReturnsAsync(acctMock.Object);
            acctMock.Setup(c => c.Deactivate()).ReturnsAsync(account.data);
            acctMock.SetupGet(c => c.Location).Returns(account.uri);
            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var args = new AccountOptions
            {
                Action = AccountAction.Deactivate,
                Path = keyPath,
            };

            var proc = new AccountCommand(args);

            Helper.SaveKey(keyPath);

            var ret = await proc.Process();
            Assert.Equal(JsonConvert.SerializeObject(account), JsonConvert.SerializeObject(ret));
        }

        [Fact]
        public async Task ShouldNotShowAccountInfoWhenNoKey()
        {
            var account = new
            {
                uri = new Uri("http://acme.d/acct/1"),
                data = new Account
                {
                    TermsOfServiceAgreed = true,
                    Contact = new[] { "mailto:admin@example.com" }
                }
            };

            var acctMock = new Mock<IAccountContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.Setup(c => c.Account()).ReturnsAsync(acctMock.Object);
            acctMock.Setup(c => c.Resource()).ReturnsAsync(account.data);
            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var proc = new AccountCommand(new AccountOptions
            {
                Action = AccountAction.Info,
                Path = "./nokey.pem"
            });

            await Assert.ThrowsAsync<Exception>(() => proc.Process());
        }

        [Fact]
        public async Task InvalidAction()
        {
            var proc = new AccountCommand(new AccountOptions
            {
                Action = (AccountAction)int.MaxValue,
            });

            await Assert.ThrowsAsync<NotSupportedException>(() => proc.Process());
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
