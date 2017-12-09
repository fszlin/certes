using System;
using System.IO;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Cli.Internal;
using Certes.Cli.Options;
using Certes.Pkcs;

namespace Certes.Cli.Processors
{
    internal class ImportCommand : CommandBase<ImportOptions>
    {
        public ImportCommand(ImportOptions options, IConsole consoleLogger)
            : base(options, consoleLogger)
        {
        }

        public override Task<AcmeContext> Process(AcmeContext context)
        {
            if (context != null && !Options.Force)
            {
                throw new Exception($"A config exists at {Options.Path}, use a different path or --force option.");
            }

            if (string.IsNullOrWhiteSpace(Options.KeyFile))
            {
                throw new Exception("Key file not specficied.");
            }

            using (var stream = File.OpenRead(Options.KeyFile))
            {
                var keyInfo = KeyInfo.From(stream);
                context = new AcmeContext
                {
                    Account = new AcmeAccount
                    {
                        Key = keyInfo
                    }
                };
            }

            return Task.FromResult(context);
        }
    }

}
