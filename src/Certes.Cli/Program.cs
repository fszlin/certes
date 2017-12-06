using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Certes.Cli.Options;
using Certes.Cli.Processors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

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

        public Program(ILogger consoleLogger)
        {
            this.consoleLogger = consoleLogger;
        }

        public static async Task Main(string[] args)
        {
            var logger = new ConsoleLogger(
                ConsoleLoggerName, (category, logLevel) => true, false);
            await new Program(logger).Process(args);
        }

        public async Task<bool> Process(string[] args)
        {
            try
            {
                ArgumentSyntax.Parse(args, syntax =>
                {
                    syntax.HandleErrors = false;
                    registerOptions = DefineRegisterCommand(syntax);
                    authorizationOptions = DefineAuthorizationCommand(syntax);
                    certificateOptions = DefineCertificateCommand(syntax);

                });
            }
            catch (ArgumentSyntaxException ex)
            {
                consoleLogger.LogError(ex.Message);
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
                        await ProcessCommand<RegisterCommand, RegisterOptions>(new RegisterCommand(registerOptions, consoleLogger));
                        break;
                    case Command.Authorization:
                        await ProcessCommand<AuthorizationCommand, AuthorizationOptions>(new AuthorizationCommand(authorizationOptions, consoleLogger));
                        break;
                    case Command.Certificate:
                        await ProcessCommand<CertificateCommand, CertificateOptions>(new CertificateCommand(certificateOptions, consoleLogger));
                        break;
                }

                return true;
            }
            catch (AggregateException ex)
            {
                foreach (var err in ex.InnerExceptions)
                {
                    consoleLogger.LogError(err, err.Message);
                }
            }
            catch (Exception ex)
            {
                consoleLogger.LogError(ex, ex.Message);
            }

            return false;
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

            syntax.DefineOption("server", ref options.Server, s => new Uri(s), $"ACME Directory Resource URI. (default: {options.Server})");
            syntax.DefineOption("p|path", ref options.Path, $"File path used to load/save the registration. (default: {options.Path})");
            syntax.DefineOption("f|force", ref options.Force, $"Force");

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

            syntax.DefineOption("server", ref options.Server, s => new Uri(s), $"ACME Directory Resource URI. (default: {options.Server})");
            syntax.DefineOption("p|path", ref options.Path, $"File path used to load/save the registration. (default: {options.Path})");
            syntax.DefineOption("f|force", ref options.Force, $"Force");

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

        private T ParseEnum<T>(string val)
        {
            return (T)Enum.Parse(typeof(T), val);
        }

        public async Task<T> Load<T>(string path)
        {
            if (!File.Exists(path))
            {
                return default;
            }

            var json = await FileUtil.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json, jsonSettings);
        }

        public async Task Save(string outputPath, object data)
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
