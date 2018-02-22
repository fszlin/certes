namespace Certes.Cli.Commands
{
    internal class CommandGroup
    {
        public static readonly CommandGroup Server = new CommandGroup
        {
            Command = "server",
            Help = Strings.HelpCommandServer
        };

        public static readonly CommandGroup Account = new CommandGroup
        {
            Command = "account",
            Help = Strings.HelpCommandAccount
        };

        public string Command { get; private set; }
        public string Help { get; private set; }

        private CommandGroup()
        {
        }
    }
}
