namespace Certes.Cli.Commands
{
    internal class CommandGroup
    {
        public static readonly CommandGroup Server = new CommandGroup { Command = "server", Help = Strings.HelpCommandServer };

        public string Command { get; private set; }
        public string Help { get; private set; }

        private CommandGroup()
        {
        }

    }
}
