using System;
using System.CommandLine;

namespace Certes.Cli
{
    internal static class ArgumentSyntaxExtensions
    {
        public static Argument<Uri> DefineOption<T>(
            this ArgumentSyntax syntax, string name, ref Uri value, string help)
            => syntax.DefineOption(name, ref value, s => new Uri(s), help);

        public static Argument<Guid> DefineOption<T>(
            this ArgumentSyntax syntax, string name, ref Guid value, string help)
            => syntax.DefineOption(name, ref value, s => new Guid(s), help);

        public static Argument<T> DefineEnumOption<T>(
            this ArgumentSyntax syntax, string name, ref T value, string help)
            => syntax.DefineOption(name, ref value, a => (T)Enum.Parse(typeof(T), a?.Replace("-", ""), true), help);

        public static Argument<T> DefineEnumParameter<T>(
            this ArgumentSyntax syntax, string name, ref T value, string help)
            => syntax.DefineParameter(name, ref value, a => (T)Enum.Parse(typeof(T), a?.Replace("-", ""), true), help);

        public static Argument<Uri> DefineOption(
            this ArgumentSyntax syntax, string name, Uri defaultValue, bool isRequired = false, string help = null)
        {
            var arg = syntax.DefineOption(name, defaultValue, s => new Uri(s), isRequired);
            arg.Help = help;
            return arg;
        }

        public static ArgumentCommand<string> DefineCommand(
            this ArgumentSyntax syntax, string name, string help = null)
        {
            var arg = syntax.DefineCommand(name);
            arg.Help = help;
            return arg;
        }
    }
}
