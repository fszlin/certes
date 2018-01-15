#if NETCOREAPP1_0 || NETCOREAPP2_0

using System.CommandLine;
using Certes.Cli.Options;
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
