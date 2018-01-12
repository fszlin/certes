using Certes.Cli.Options;
using Certes.Cli.Processors;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Certes.Cli
{
    public class Program
    {
        internal const string ConsoleLoggerName = "certes-cli-console-logger";
        private readonly ILogger consoleLogger;

        private JsonSerializerSettings jsonSettings;
        private Command command = Command.Undefined;
        private RegisterOptions registerOptions;
        private Formatting formatting = Formatting.None;
        private AuthorizationOptions authorizationOptions;
        private CertificateOptions certificateOptions;
        private ImportOptions importOptions;

        public Program(ILogger consoleLogger)
        {
            this.consoleLogger = consoleLogger;
        }

        public static async Task<int> Main(string[] args)
        {
            using (var factory = new LogFactory(ConfigureConsoleLogger()))
            {
                var logger = factory.GetLogger(ConsoleLoggerName);
                var succeed = await new Program(logger).Process(args);
                return succeed ? 0 : 1;
            }
        }

        public async Task<bool> Process(string[] args)
        {
            try
            {
                ArgumentSyntax.Parse(args, syntax =>
                {
                    syntax.ApplicationName = "certes";
                    syntax.HandleErrors = false;
                    registerOptions = DefineRegisterCommand(syntax);
                    authorizationOptions = DefineAuthorizationCommand(syntax);
                    certificateOptions = DefineCertificateCommand(syntax);
                    importOptions = DefineImportCommand(syntax);

                });
            }
            catch (ArgumentSyntaxException ex)
            {
                consoleLogger.Error(ex, ex.Message);
                return false;
            }

            jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

            jsonSettings.Converters.Add(new StringEnumConverter());

#if DEBUG
            formatting = Formatting.Indented;
#endif
            
            try
            {
                switch (command)
                {
                    case Command.Register:
                        await ProcessCommand<RegisterCommand, RegisterOptions>(
                            new RegisterCommand(registerOptions, consoleLogger));
                        break;
                    case Command.Authorization:
                        await ProcessCommand<AuthorizationCommand, AuthorizationOptions>(
                            new AuthorizationCommand(authorizationOptions, consoleLogger));
                        break;
                    case Command.Certificate:
                        await ProcessCommand<CertificateCommand, CertificateOptions>(
                            new CertificateCommand(certificateOptions, consoleLogger));
                        break;
                    case Command.Import:
                        await ProcessCommand<ImportCommand, ImportOptions>(
                            new ImportCommand(importOptions, consoleLogger));
                        break;
                }

                return true;
            }
            catch (AggregateException ex)
            {
                foreach (var err in ex.InnerExceptions)
                {
                    consoleLogger.Error(err, err.Message);
                }
            }
            catch (Exception ex)
            {
                consoleLogger.Error(ex, ex.Message);
            }

            return false;
        }

        private ImportOptions DefineImportCommand(ArgumentSyntax syntax)
        {
            var options = new ImportOptions();
            syntax.DefineCommand("import", ref command, Command.Import, "Import ACME account");

            syntax.DefineOption("key-file", ref options.KeyFile, "The path to the account key.");

            DefineCommonOptions(options, syntax);

            return options;
        }

        private static LoggingConfiguration ConfigureConsoleLogger()
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget
            {
                Layout = @"${message}"
            };

            config.AddTarget(ConsoleLoggerName, consoleTarget);

            var consoleRule = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(consoleRule);

            return config;
        }

        private CertificateOptions DefineCertificateCommand(ArgumentSyntax syntax)
        {
            var options = new CertificateOptions();
            syntax.DefineCommand("cert", ref command, Command.Certificate, "Authorization");

            syntax.DefineOption("n|name", ref options.Name, "Friendly name of the cert.");

            syntax.DefineOption("dn|distinguished-name", ref options.DistinguishedName, $"The distinguished name of the cert.");
            syntax.DefineOptionList("v|value", ref options.Values, $"The distinguished name of the cert.");
            syntax.DefineOption("value-path", ref options.ValuesFile, $"The subject alt names.");

            syntax.DefineOption("cer|export-cer", ref options.ExportCer, "Path to export PEM cer.");
            syntax.DefineOption("export-key", ref options.ExportKey, "Path to export PEM key.");
            syntax.DefineOption("pfx|export-pfx", ref options.ExportPfx, "Path to export pfx.");
            syntax.DefineOption("revoke", ref options.RevokeCer, "Revoke certificate.");
            syntax.DefineOption("pw|password", ref options.Password, "Password for the pfx.");
            syntax.DefineOption("full-chain-off", ref options.NoChain, "Skip full cert chain.");

            DefineCommonOptions(options, syntax);

            return options;
        }

        private AuthorizationOptions DefineAuthorizationCommand(ArgumentSyntax syntax)
        {
            var options = new AuthorizationOptions();
            syntax.DefineCommand("authz", ref command, Command.Authorization, "Perform identifier authorization.");

            syntax.DefineOption("t|type", ref options.Type, "Type of authorization challenge to process. (default: None)");
            syntax.DefineOptionList("v|value", ref options.Values, $"One or more names for authorization.");

            syntax.DefineOption("value-path", ref options.ValuesFile, $"The path of file contains one or more names for authorization.");
            syntax.DefineOption("complete-authz", ref options.Complete, $"Complete authz.");
            syntax.DefineOption("k|key-authz", ref options.KeyAuthentication, $"Print key authz.");
            syntax.DefineOption("r|refresh", ref options.Refresh, $"Print key authz.");

            DefineCommonOptions(options, syntax);

            return options;
        }

        private RegisterOptions DefineRegisterCommand(ArgumentSyntax syntax)
        {
            var options = new RegisterOptions();
            syntax.DefineCommand("register", ref command, Command.Register, "Create a new registration.");

            syntax.DefineOption("m|email", ref options.Email, "Email used for registration and recovery contact. (default: None)");
            syntax.DefineOption("register-unsafely-without-email", ref options.NoEmail, "Create registration without email.");
            syntax.DefineOption("agree-tos", ref options.AgreeTos, $"Agree to the ACME Subscriber Agreement (default: {options.AgreeTos})");
            syntax.DefineOption("update-registration", ref options.Update, $"With the register verb, indicates that details associated with an existing registration, such as the e-mail address, should be updated, rather than registering a new account. (default: None)");
            syntax.DefineOption("thumbprint", ref options.Thumbprint, $"Print thumbprint of the account.");
            
            DefineCommonOptions(options, syntax);

            return options;
        }

        private T DefineCommonOptions<T>(T options, ArgumentSyntax syntax)
            where T: OptionsBase
        {
            syntax.DefineOption("server", ref options.Server, s => new Uri(s), $"ACME Directory Resource URI. (default: {options.Server})");
            syntax.DefineOption("p|path", ref options.Path, $"File path used to load/save the registration. (default: {options.Path})");
            syntax.DefineOption("f|force", ref options.Force, $"If registering new account, overwrite the existing configuration if needed.");

            return options;
        }

        private async Task ProcessCommand<TCommand, TOptions>(TCommand command)
            where TCommand : CommandBase<TOptions>
            where TOptions : OptionsBase
        {
            var options = command.Options;
            var context = await Load<AcmeContext>(options.Path);
            context = await command.Process(context);
            await Save(options.Path, context);
        }

        private async Task<T> Load<T>(string path)
        {
            if (!File.Exists(path))
            {
                return default;
            }

            var json = await FileUtil.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json, jsonSettings);
        }

        private async Task Save(string outputPath, object data)
        {
            var json = JsonConvert.SerializeObject(data, formatting, jsonSettings);
            var dir = new DirectoryInfo(Path.GetDirectoryName(outputPath));
            if (!dir.Exists)
            {
                dir.Create();
            };

            using (var output = File.Create(outputPath))
            {
                using (var streamWriter = new StreamWriter(output))
                {
                    await streamWriter.WriteAsync(json);
                }
            }

        }
    }
}
