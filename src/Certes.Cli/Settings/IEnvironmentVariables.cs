namespace Certes.Cli.Settings
{
    internal interface IEnvironmentVariables
    {
        string GetVar(string name);
    }
}
