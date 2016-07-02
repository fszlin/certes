using Certes.Acme;
using Certes.Cli.Options;
using Certes.Pkcs;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Cli.Processors
{
    internal class CertificateCommand : CommandBase<CertificateOptions>
    {
        private static readonly char[] NameValueSeparator = new[] { '\r', '\n', ' ', ';', ',' };

        public CertificateCommand(CertificateOptions options, ILogger consoleLogger)
            : base(options, consoleLogger)
        {
        }

        public override async Task<AcmeContext> Process(AcmeContext context)
        {
            if (context?.Account == null)
            {
                throw new Exception("Account not specified.");
            }

            if ( string.IsNullOrWhiteSpace(Options.Name ))
            {
                throw new Exception("Certificate name not specficied.");
            }

            if (context.Certificates == null)
            {
                context.Certificates = new Dictionary<string, AcmeCertificate>();
            }

            if (!string.IsNullOrWhiteSpace(Options.ExportCer))
            {
                var cert = context.Certificates.TryGet(Options.Name);
                if (cert == null)
                {
                    throw new Exception($"Certificate {Options.Name} not found.");
                }

                if (cert.Raw == null)
                {
                    throw new Exception($"Certificate {Options.Name} not ready.");
                }

                await FileUtil.WriteAllBytes(Options.ExportCer, cert.Raw);
            }
            else if (!string.IsNullOrWhiteSpace(Options.ExportPfx))
            {
                var cert = context.Certificates.TryGet(Options.Name);
                if (cert == null)
                {
                    throw new Exception($"Cert {Options.Name} not found.");
                }

                if (cert.Raw == null)
                {
                    throw new Exception($"Certificate {Options.Name} not ready.");
                }

                var pfxBuilder = new PfxBuilder(cert.Raw, cert.Key);
                var issuer = cert.Issuer;
                while (issuer != null)
                {
                    pfxBuilder.AddIssuer(issuer.Raw);
                    issuer = issuer.Issuer;
                }

                var pfx = pfxBuilder.Build(Options.Name, Options.Password);
                await FileUtil.WriteAllBytes(Options.ExportPfx, pfx);
            }
            else if (!string.IsNullOrWhiteSpace(Options.ExportKey))
            {
                var cert = context.Certificates.TryGet(Options.Name);
                if (cert == null)
                {
                    throw new Exception($"Cert {Options.Name} not found.");
                }

                using (var stream = File.Create(Options.ExportKey))
                {
                    cert.Key.Save(stream);
                }
            }
            else if (Options.RevokeCer)
            {
                var cert = context.Certificates.TryGet(Options.Name);
                if (cert == null)
                {
                    throw new Exception($"Cert {Options.Name} not found.");
                }

                using (var client = new AcmeClient(Options.Server))
                {
                    client.Use(context.Account.Key);

                    cert = await client.RevokeCertificate(cert);

                    context.Certificates[Options.Name] = cert;
                }
            }
            else
            {
                if (!Options.Force && context.Certificates.ContainsKey(Options.Name))
                {
                    throw new Exception($"Certificate {Options.Name} already exists. Use --force to overwrite.");
                }

                var values = await GetAllValues();
                var csrBuilder = new CertificationRequestBuilder();
                csrBuilder.AddName(Options.DistinguishedName);

                foreach (var value in values)
                {
                    csrBuilder.SubjectAlternativeNames.Add(value);
                }

                using (var client = new AcmeClient(Options.Server))
                {
                    client.Use(context.Account.Key);

                    var cert = await client.NewCertificate(csrBuilder);

                    context.Certificates[Options.Name] = cert;
                }
            }

            return context;
        }

        private async Task<string[]> GetAllValues()
        {
            var values = Options.Values.ToArray();

            if (!string.IsNullOrWhiteSpace(Options.ValuesFile))
            {
                if (!File.Exists(Options.ValuesFile))
                {
                    throw new Exception($"{Options.ValuesFile} not exist.");
                }

                var text = await FileUtil.ReadAllText(Options.ValuesFile);

                var names = text.Split(NameValueSeparator);
                values = values.Union(names)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .Select(n => n.Trim())
                    .ToArray();
            }

            return values;
        }

    }
}
