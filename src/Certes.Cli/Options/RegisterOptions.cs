namespace Certes.Cli.Options
{
    internal class RegisterOptions : OptionsBase
    {
        public string Email = "";
        public bool AgreeTos = false;
        public bool Update = false;
        public bool Thumbprint = false;
        public bool NoEmail = false;
    }
}
