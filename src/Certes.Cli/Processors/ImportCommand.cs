using System;
using System.IO;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Cli.Options;
using Certes.Pkcs;
using NLog;

namespace Certes.Cli.Processors
{
    internal class ImportCommand : CommandBase<ImportOptions>
    {
        public ImportCommand(ImportOptions options)
            : base(options)
        {
        }

        public override Task<CliContext> Process(CliContext context)
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
                context = new CliContext
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
