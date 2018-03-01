using System;
using System.CommandLine;
using System.Linq;
using Certes.Acme;

namespace Certes.Cli
{
    internal static class ArgumentSyntaxExtensions
    {
        public const string ServerOptionName = "server";
        public const string KeyOptionName = "key";
        
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
            var values = syntax.GetActiveOptions()
                .OfType<Argument<T>>()
                .Where(a => name.Equals(a.Name, StringComparison.Ordinal))
                .Select(a => a.Value);

            if (isRequired && values.All(v => Equals(v, default(T))))
            {
                syntax.ReportError(string.Format(Strings.ErrorOptionMissing, name));
            }

            return values.FirstOrDefault();
        }

        public static T GetParameter<T>(this ArgumentSyntax syntax, string name, bool isRequired = false)
        {
            var values = syntax.GetActiveArguments()
                .OfType<Argument<T>>()
                .Where(a => name.Equals(a.Name, StringComparison.Ordinal))
                .Select(a => a.Value);

            if (isRequired && values.All(v => Equals(v, default(T))))
            {
                syntax.ReportError(string.Format(Strings.ErrorParameterMissing, name));
            }

            return values.FirstOrDefault();
        }
    }
}
