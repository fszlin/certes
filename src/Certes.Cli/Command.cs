namespace Certes.Cli
{
    public enum Command
    {
        Undefined,

        // ACME v1 commands
        Register,
        Authorization,
        Certificate,
        Import,

        // ACME v2 commands
        Account,
        Order,
        Azure,
    }
}
