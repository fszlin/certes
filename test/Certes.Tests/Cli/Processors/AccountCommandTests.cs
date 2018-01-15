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

            options = Parse("account set --path ./account-key.pem");
            Assert.Equal(AccountAction.Set, options.Action);
            Assert.Equal("./account-key.pem", options.Path);
        }

        private AccountOptions Parse(string cmd)
        {
            AccountOptions options = null;
            ArgumentSyntax.Parse(cmd.Split(' '), syntax =>
            {
                syntax.DefineCommand("noop");
                options = AccountCommand.TryParse(syntax);
            });

            return options;
        }
    }
}

#endif
