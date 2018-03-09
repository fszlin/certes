using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Settings;

namespace Certes.Cli
{
    internal static class ArgumentSyntaxExtensions
    {
        public const string ServerOptionName = "s|server";
        public const string KeyOptionName = "k|key";
        
        public static ArgumentSyntax DefineServerOption(this ArgumentSyntax syntax)
        {
            Uri value = null;
            var arg = syntax.DefineOption(ServerOptionName, value, s => new Uri(s));
            arg.Help = Strings.HelpServer;
            return syntax;
        }

        public static ArgumentSyntax DefineOption(this ArgumentSyntax syntax, string name, string help)
        {
            string value = null;
            var opt = syntax.DefineOption(name, ref value, true, help);
            return syntax;
        }

        public static ArgumentSyntax DefineKeyOption(this ArgumentSyntax syntax)
            => syntax.DefineOption(KeyOptionName, help: Strings.HelpKey);

        public static ArgumentCommand<string> DefineCommand(
            this ArgumentSyntax syntax, string name, string help = null)
        {
            var arg = syntax.DefineCommand(name);
            arg.Help = help;
            return arg;
        }

        public static ArgumentSyntax DefineUriParameter(
            this ArgumentSyntax syntax, string name, string help = null)
        {
            var arg = syntax.DefineParameter(name, null, s => new Uri(s));
            arg.Help = help;
            return syntax;
        }

        public static ArgumentSyntax DefineParameter(
            this ArgumentSyntax syntax, string name, string help = null)
        {
            var arg = syntax.DefineParameter(name, null);
            arg.Help = help;
            return syntax;
        }

        public static Uri GetServerOption(this ArgumentSyntax syntax)
            => syntax.GetOption<Uri>(ServerOptionName);
        public static string GetKeyOption(this ArgumentSyntax syntax)
            => syntax.GetOption<string>(KeyOptionName);

        public static T GetOption<T>(this ArgumentSyntax syntax, string name, bool isRequired = false)
        {
            var firstName = name.Split('|').First();
            var values = syntax.GetActiveOptions()
                .OfType<Argument<T>>()
                .Where(a => firstName.Equals(a.Name, StringComparison.Ordinal))
                .Select(a => a.Value);

            if (isRequired && values.All(v => Equals(v, default(T))))
            {
                syntax.ReportError(string.Format(Strings.ErrorOptionMissing, name));
            }

            return values.FirstOrDefault();
        }

        public static T GetParameter<T>(this ArgumentSyntax syntax, string name, bool isRequired = false)
        {
            var firstName = name.Split('|').First();
            var values = syntax.GetActiveArguments()
                .OfType<Argument<T>>()
                .Where(a => firstName.Equals(a.Name, StringComparison.Ordinal))
                .Select(a => a.Value);

            if (isRequired && values.All(v => Equals(v, default(T))))
            {
                syntax.ReportError(string.Format(Strings.ErrorParameterMissing, name));
            }

            return values.FirstOrDefault();
        }

        public static async Task<IKey> ReadKey(
            this ArgumentSyntax syntax,
            string optionName,
            string environmentVariableName,
            IFileUtil file,
            IEnvironmentVariables environment,
            bool isRequired = false)
        {
            var keyPath = syntax.GetOption<string>(optionName);
            if (!string.IsNullOrWhiteSpace(keyPath))
            {
                return KeyFactory.FromPem(await file.ReadAllText(keyPath));
            }
            else
            {
                var keyData = environment.GetVar(environmentVariableName);
                if (!string.IsNullOrWhiteSpace(keyData))
                {
                    return KeyFactory.FromDer(Convert.FromBase64String(keyData));
                }
                else if (isRequired)
                {
                    throw new Exception(string.Format(Strings.ErrorOptionMissing, optionName));
                }
            }

            return null;
        }
    }
}
