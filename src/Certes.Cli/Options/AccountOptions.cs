namespace Certes.Cli.Options
{
    internal class AccountOptions : OptionsV2Base
    {
        public AccountAction Action;
        public string Email = "";
        public bool AgreeTos = false;
    }
}
