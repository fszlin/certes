using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Commands;
using Certes.Cli.Settings;
using Certes.Json;
using Certes.Pkcs;
using NLog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Certes.Cli
{
    /// <summary>
    /// Spectre.Console.Cli-based CLI implementation.
    /// Spectre handles command parsing and dispatch.
    /// </summary>
    internal class CliCoreSpectre
    {
        private readonly ILogger consoleLogger = LogManager.GetLogger(nameof(CliCoreSpectre));
        private static readonly JsonSerializerOptions jsonSerializerSettings = JsonUtil.CreateSettings();
        private readonly IUserSettings userSettings;
        private readonly AcmeContextFactory contextFactory;
        private readonly IFileUtil fileUtil;
        private readonly IEnvironmentVariables environmentVariables;

        public CliCoreSpectre(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil,
            IEnvironmentVariables environmentVariables)
        {
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
            this.fileUtil = fileUtil;
            this.environmentVariables = environmentVariables;
        }

        /// <summary>
        /// Runs the CLI using Spectre for parsing and dispatch.
        /// </summary>
        public async Task<bool> Run(string[] args, CancellationToken cancellationToken = default)
            => await RunWithExitCode(args, cancellationToken) == 0;

        internal async Task<int> RunWithExitCode(string[] args, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var app = new CommandApp();
                app.Configure(config =>
                {
                    config.SetApplicationName("certes");
                    config.PropagateExceptions();

                    config.Settings.StrictParsing = false;
                    config.Settings.ConvertFlagsToRemainingArguments = true;

                    ConfigureServerBranch(config);
                    ConfigureAccountBranch(config);
                    ConfigureOrderBranch(config);
                    ConfigureCertificateBranch(config);
                });

                var result = await app.RunAsync(args, cancellationToken);
                return result == 0 ? 0 : 1;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                consoleLogger.Info("Operation cancelled.");
                return 130;
            }
            catch (Exception ex)
            {
                consoleLogger.Error(ex.Message);
                consoleLogger.Debug(ex);
                return 1;
            }
        }

        private void ConfigureServerBranch(IConfigurator config)
        {
            config.AddBranch(CommandGroup.Server.Command, branch =>
            {
                branch.AddAsyncDelegate<ServerSetSettings>("set", async (_, settings, cancellationToken) =>
                {
                    var ctx = contextFactory.Invoke(settings.NewServer, null);
                    consoleLogger.Debug("Loading directory from '{0}'", settings.NewServer);
                    var directory = await ctx.GetDirectory(cancellationToken);
                    await userSettings.SetDefaultServer(settings.NewServer, cancellationToken);

                    WriteJson(new
                    {
                        location = settings.NewServer,
                        resource = directory,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandServerSet);

                branch.AddAsyncDelegate<ServerShowSettings>("show", async (_, settings, cancellationToken) =>
                {
                    var serverUri = settings.Server ?? await userSettings.GetDefaultServer(cancellationToken);
                    var ctx = contextFactory.Invoke(serverUri, null);
                    consoleLogger.Debug("Loading directory from '{0}'", serverUri);
                    var directory = await ctx.GetDirectory(cancellationToken);

                    WriteJson(new
                    {
                        location = serverUri,
                        resource = directory,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandServerShow);
            });
        }

        private void ConfigureAccountBranch(IConfigurator config)
        {
            config.AddBranch(CommandGroup.Account.Command, branch =>
            {
                branch.AddAsyncDelegate<AccountNewSettings>("new", async (_, settings, cancellationToken) =>
                {
                    var account = await ReadAccountKey(settings.Server, cancellationToken, settings.KeyPath);
                    var key = account.Key ?? KeyFactory.NewKey(KeyAlgorithm.ES256);

                    consoleLogger.Debug("Creating new account on '{0}'.", account.Server);
                    var acme = contextFactory.Invoke(account.Server, key);
                    var acctCtx = await acme.NewAccount(settings.Email, true, cancellationToken: cancellationToken);

                    // Once the CA accepted the account, persist its key even if
                    // cancellation raced with the response. Do not lose credentials.

                    if (!string.IsNullOrWhiteSpace(settings.OutPath))
                    {
                        consoleLogger.Debug("Saving new account key to '{0}'.", settings.OutPath);
                        await fileUtil.WriteAllText(settings.OutPath, key.ToPem(), CancellationToken.None);
                    }
                    else
                    {
                        consoleLogger.Debug("Saving new account key to user settings.");
                        await userSettings.SetAccountKey(account.Server, key, CancellationToken.None);
                    }

                    await WriteCreatedResource(acctCtx, cancellationToken);

                    return 0;
                }).WithDescription(Strings.HelpCommandAccountNew);

                branch.AddAsyncDelegate<AccountSetSettings>("set", async (_, settings, cancellationToken) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, cancellationToken, settings.KeyPath, required: true);

                    consoleLogger.Debug("Setting account for '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var acctCtx = await acme.Account(cancellationToken);
                    await userSettings.SetAccountKey(serverUri, key, cancellationToken);

                    WriteJson(new
                    {
                        location = acctCtx.Location,
                        resource = await acctCtx.Resource(cancellationToken),
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandAccountSet);

                branch.AddAsyncDelegate<AccountShowSettings>("show", async (_, settings, cancellationToken) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, cancellationToken, settings.KeyPath, fallbackToSettings: true, required: true);

                    consoleLogger.Debug("Loading account from '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var acctCtx = await acme.Account(cancellationToken);

                    WriteJson(new
                    {
                        location = acctCtx.Location,
                        resource = await acctCtx.Resource(cancellationToken),
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandAccountShow);

                branch.AddAsyncDelegate<AccountUpdateSettings>("update", async (_, settings, cancellationToken) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, cancellationToken, settings.KeyPath, fallbackToSettings: true, required: true);

                    consoleLogger.Debug("Updating account on '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var acctCtx = await acme.Account(cancellationToken);
                    var account = await acctCtx.Update(new[] { $"mailto://{settings.Email}" }, true, cancellationToken);

                    WriteJson(new
                    {
                        location = acctCtx.Location,
                        resource = account,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandAccountUpdate);
            });
        }

        private async Task<(Uri Server, IKey Key)> ReadAccountKey(
            Uri server,
            CancellationToken cancellationToken,
            string keyPath = null,
            bool fallbackToSettings = false,
            bool required = false)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var serverUri = server ?? await userSettings.GetDefaultServer(cancellationToken);

            if (!string.IsNullOrWhiteSpace(keyPath))
            {
                consoleLogger.Debug("Load account key form '{0}'.", keyPath);
                var pem = await fileUtil.ReadAllText(keyPath, cancellationToken);
                return (serverUri, KeyFactory.FromPem(pem));
            }

            var key = fallbackToSettings
                ? await userSettings.GetAccountKey(serverUri, cancellationToken)
                : null;

            if (required && key == null)
            {
                throw new CertesCliException(string.Format(Strings.ErrorNoAccountKey, serverUri));
            }

            return (serverUri, key);
        }

        // Orders created from plain strings may contain IP identifiers (RFC 8738), so fall back
        // to an IP lookup, which matches by address, when no DNS authorization matches.
        private static async Task<IAuthorizationContext> FindAuthorization(IOrderContext orderCtx, string value, CancellationToken cancellationToken)
            => await orderCtx.Authorization(value, cancellationToken: cancellationToken)
                ?? await orderCtx.Authorization(value, IdentifierType.Ip, cancellationToken)
                ?? throw new CertesCliException(string.Format(Strings.ErrorIdentifierNotAvailable, value));

        private static string ParseChallengeType(string value)
            => value?.ToLowerInvariant() switch
            {
                "dns" or "dns-01" => ChallengeTypes.Dns01,
                "http" or "http-01" => ChallengeTypes.Http01,
                "tls-alpn" or "tls-alpn-01" => ChallengeTypes.TlsAlpn01,
                _ => throw new CertesCliException(string.Format(Strings.ErrorInvalidChallengeType, value)),
            };

        private void ConfigureOrderBranch(IConfigurator config)
        {
            config.AddBranch(CommandGroup.Order.Command, branch =>
            {
                branch.AddAsyncDelegate<OrderNewSettings>("new", async (_, settings, cancellationToken) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, cancellationToken, settings.KeyPath, fallbackToSettings: true);
                    consoleLogger.Debug("Creating order from '{0}'.", serverUri);

                    var acme = contextFactory.Invoke(serverUri, key);
                    // Empty values are rejected in OrderNewSettings.Validate, so null means "not supplied".
                    var orderCtx =
                        settings.Profile != null
                            ? await acme.NewOrderWithProfile(settings.Domains, settings.Profile, replacedCertificateId: settings.Replaces, cancellationToken: cancellationToken)
                        : settings.Replaces != null
                            ? await acme.NewReplacementOrder(settings.Domains, settings.Replaces, cancellationToken: cancellationToken)
                        : await acme.NewOrder(settings.Domains, cancellationToken: cancellationToken);

                    await WriteCreatedResource(orderCtx, cancellationToken);

                    return 0;
                }).WithDescription(Strings.HelpCommandOrderNew);

                branch.AddAsyncDelegate<OrderListSettings>("list", async (_, settings, cancellationToken) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, cancellationToken, settings.KeyPath, fallbackToSettings: true);
                    consoleLogger.Debug("Loading orders from '{0}'.", serverUri);

                    var acme = contextFactory.Invoke(serverUri, key);
                    var acctCtx = await acme.Account(cancellationToken);
                    var orderListCtx = await acctCtx.Orders(cancellationToken);
                    var orderList = new List<object>();

                    foreach (var orderCtx in await orderListCtx.Orders(cancellationToken))
                    {
                        orderList.Add(new
                        {
                            location = orderCtx.Location,
                            resource = await orderCtx.Resource(cancellationToken),
                        });
                    }

                    WriteJson(orderList);
                    return 0;
                }).WithDescription(Strings.HelpCommandOrderList);

                branch.AddAsyncDelegate<OrderShowSettings>("show", async (_, settings, cancellationToken) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, cancellationToken, settings.KeyPath, fallbackToSettings: true);
                    consoleLogger.Debug("Loading order from '{0}'.", serverUri);

                    var acme = contextFactory.Invoke(serverUri, key);
                    var orderCtx = acme.Order(settings.OrderId);

                    WriteJson(new
                    {
                        location = orderCtx.Location,
                        resource = await orderCtx.Resource(cancellationToken),
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandOrderShow);

                branch.AddAsyncDelegate<OrderAuthzSettings>("authz", async (_, settings, cancellationToken) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, cancellationToken, settings.KeyPath, fallbackToSettings: true);
                    var type = ParseChallengeType(settings.ChallengeType);

                    consoleLogger.Debug("Loading authz from '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var orderCtx = acme.Order(settings.OrderId);
                    var authzCtx = await FindAuthorization(orderCtx, settings.Domain, cancellationToken);
                    var challengeCtx = await authzCtx.Challenge(type, cancellationToken)
                        ?? throw new CertesCliException(string.Format(Strings.ErrorChallengeNotAvailable, type));

                    var challenge = await challengeCtx.Resource(cancellationToken);
                    if (string.Equals(type, ChallengeTypes.Dns01, StringComparison.OrdinalIgnoreCase))
                    {
                        WriteJson(new
                        {
                            location = challengeCtx.Location,
                            dnsTxt = key.DnsTxt(challenge.Token),
                            resource = challenge,
                        });
                    }
                    else if (string.Equals(type, ChallengeTypes.TlsAlpn01, StringComparison.OrdinalIgnoreCase))
                    {
                        // RFC 8737: serve a self-signed certificate carrying the SHA-256 digest of keyAuthz
                        // in the critical acmeIdentifier extension, over ALPN protocol "acme-tls/1".
                        WriteJson(new
                        {
                            location = challengeCtx.Location,
                            keyAuthz = challengeCtx.KeyAuthz,
                            resource = challenge,
                        });
                    }
                    else
                    {
                        WriteJson(new
                        {
                            location = challengeCtx.Location,
                            challengeFile = $".well-known/acme-challenge/{challenge.Token}",
                            challengeTxt = $"{challenge.Token}.{key.Thumbprint()}",
                            resource = challenge,
                            keyAuthz = challengeCtx.KeyAuthz,
                        });
                    }

                    return 0;
                }).WithDescription(Strings.HelpCommandOrderAuthz);

                branch.AddAsyncDelegate<OrderValidateSettings>("validate", async (_, settings, cancellationToken) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, cancellationToken, settings.KeyPath, fallbackToSettings: true);
                    var type = ParseChallengeType(settings.ChallengeType);

                    consoleLogger.Debug("Validating authz on '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var orderCtx = acme.Order(settings.OrderId);
                    var authzCtx = await FindAuthorization(orderCtx, settings.Domain, cancellationToken);
                    var challengeCtx = await authzCtx.Challenge(type, cancellationToken)
                        ?? throw new CertesCliException(string.Format(Strings.ErrorChallengeNotAvailable, settings.ChallengeType));

                    consoleLogger.Debug("Validating challenge '{0}'.", challengeCtx.Location);
                    var challenge = await challengeCtx.Validate(cancellationToken);

                    WriteJson(new
                    {
                        location = challengeCtx.Location,
                        resource = challenge,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandOrderValidate);

                branch.AddAsyncDelegate<OrderFinalizeSettings>("finalize", async (_, settings, cancellationToken) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, cancellationToken, settings.KeyPath, fallbackToSettings: true);
                    var providedKey = await ReadKey(settings.PrivateKey, "CERTES_CERT_KEY", cancellationToken);
                    var certificateKey = providedKey ?? KeyFactory.NewKey(settings.KeyAlgorithm);

                    consoleLogger.Debug("Finalizing order from '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var orderCtx = acme.Order(settings.OrderId);
                    var csr = await orderCtx.CreateCsr(certificateKey, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(settings.Dn))
                    {
                        csr.AddName(settings.Dn);
                    }

                    var order = await orderCtx.Finalize(csr.Generate(), cancellationToken);

                    if (string.IsNullOrWhiteSpace(settings.OutPath) && providedKey == null)
                    {
                        WriteJson(new
                        {
                            location = orderCtx.Location,
                            privateKey = certificateKey.ToDer(),
                            resource = order,
                        });
                    }
                    else
                    {
                        if (providedKey == null)
                        {
                            // Preserve a generated key after successful finalization,
                            // even if cancellation raced with the CA response.
                            await fileUtil.WriteAllText(settings.OutPath, certificateKey.ToPem(), CancellationToken.None);
                        }

                        WriteJson(new
                        {
                            location = orderCtx.Location,
                            resource = order,
                        });
                    }

                    return 0;
                }).WithDescription(Strings.HelpCommandOrderFinalize);
            });
        }

        private void ConfigureCertificateBranch(IConfigurator config)
        {
            config.AddBranch(CommandGroup.Certificate.Command, branch =>
            {
                branch.AddAsyncDelegate<CertificatePemSettings>("pem", async (_, settings, cancellationToken) =>
                {
                    var (location, cert) = await DownloadCertificate(settings.OrderId, settings.PreferredChain, settings.Server, settings.KeyPath, cancellationToken);

                    if (string.IsNullOrWhiteSpace(settings.OutPath))
                    {
                        WriteJson(new
                        {
                            location,
                            resource = new
                            {
                                certificate = cert.Certificate.ToDer(),
                                issuers = cert.Issuers.Select(i => i.ToDer()).ToArray(),
                            },
                        });
                    }
                    else
                    {
                        consoleLogger.Debug("Saving certificate to '{0}'.", settings.OutPath);
                        await fileUtil.WriteAllText(settings.OutPath, cert.ToPem(), cancellationToken);

                        WriteJson(new
                        {
                            location,
                        });
                    }

                    return 0;
                }).WithDescription(Strings.HelpCommandCertificatePem);

                branch.AddAsyncDelegate<CertificateRenewalInfoSettings>("renewal-info", async (_, settings, cancellationToken) =>
                {
                    var (serverUri, _) = await ReadAccountKey(settings.Server, cancellationToken);
                    consoleLogger.Debug("Loading certificate from '{0}'.", settings.CertPath);
                    var chain = new CertificateChain(await fileUtil.ReadAllText(settings.CertPath, cancellationToken));
                    var certificateId = chain.GetRenewalInfoCertificateId();

                    consoleLogger.Debug("Loading renewal information from '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, null);
                    var info = await acme.GetRenewalInfo(certificateId, cancellationToken);

                    WriteJson(new
                    {
                        certificateId,
                        suggestedWindow = info.SuggestedWindow,
                        explanationUrl = info.ExplanationUrl,
                        retryAfterSeconds = info.RetryAfter?.TotalSeconds,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandCertificateRenewalInfo);

                branch.AddAsyncDelegate<CertificatePfxSettings>("pfx", async (_, settings, cancellationToken) =>
                {
                    var (location, cert) = await DownloadCertificate(settings.OrderId, settings.PreferredChain, settings.Server, settings.KeyPath, cancellationToken);
                    var privKey = await ReadKey(settings.PrivateKey, "CERTES_CERT_KEY", cancellationToken);
                    if (privKey == null)
                    {
                        throw new CertesCliException(Strings.ErrorNoPrivateKey);
                    }

                    var pfxName = string.Format(CultureInfo.InvariantCulture, "[certes] {0:yyyyMMddhhmmss}", DateTime.UtcNow);
                    if (!string.IsNullOrWhiteSpace(settings.FriendlyName))
                    {
                        pfxName = string.Concat(settings.FriendlyName, " ", pfxName);
                    }

                    var pfxBuilder = cert.ToPfx(privKey);
                    if (settings.LegacyEncryption)
                    {
                        pfxBuilder.Encryption = PfxEncryption.Legacy;
                    }

                    if (!string.IsNullOrWhiteSpace(settings.Issuer))
                    {
                        var issuerPem = await fileUtil.ReadAllText(settings.Issuer, cancellationToken);
                        pfxBuilder.AddIssuers(Encoding.UTF8.GetBytes(issuerPem));
                    }

                    var pfx = pfxBuilder.Build(pfxName, settings.Password);
                    if (string.IsNullOrWhiteSpace(settings.OutPath))
                    {
                        WriteJson(new
                        {
                            location,
                            pfx,
                        });
                    }
                    else
                    {
                        consoleLogger.Debug("Saving certificate to '{0}'.", settings.OutPath);
                        await fileUtil.WriteAllBytes(settings.OutPath, pfx, cancellationToken);

                        WriteJson(new
                        {
                            location,
                        });
                    }

                    return 0;
                }).WithDescription(Strings.HelpCommandCertificatePfx);
            });
        }

        private async Task<(Uri Location, CertificateChain Cert)> DownloadCertificate(Uri orderUri, string preferredChain, Uri server, string keyPath, CancellationToken cancellationToken)
        {
            var (serverUri, key) = await ReadAccountKey(server, cancellationToken, keyPath, fallbackToSettings: true, required: true);

            consoleLogger.Debug("Downloading certificate from '{0}'.", serverUri);
            var acme = contextFactory.Invoke(serverUri, key);
            var orderCtx = acme.Order(orderUri);
            var order = await orderCtx.Resource(cancellationToken);
            if (order.Status != OrderStatus.Valid)
            {
                throw new CertesCliException(string.Format(Strings.ErrorExportInvalidOrder, order.Status));
            }

            return (order.Certificate, await orderCtx.Download(preferredChain, cancellationToken));
        }

        private static void WriteJson(object value)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, jsonSerializerSettings));
        }

        private static async Task WriteCreatedResource<T>(IResourceContext<T> context, CancellationToken cancellationToken)
        {
            T resource;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                resource = await context.Resource(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Creation succeeded; retain the only recovery handle even when
                // cancellation prevents the follow-up resource lookup.
                WriteJson(new { location = context.Location });
                throw;
            }
            WriteJson(new { location = context.Location, resource });
        }

        private async Task<IKey> ReadKey(string keyPath, string environmentVariableName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(keyPath))
            {
                return KeyFactory.FromPem(await fileUtil.ReadAllText(keyPath, cancellationToken));
            }

            var keyData = environmentVariables.GetVar(environmentVariableName);
            if (!string.IsNullOrWhiteSpace(keyData))
            {
                return KeyFactory.FromDer(Convert.FromBase64String(keyData));
            }

            return null;
        }
    }

    internal sealed class ServerSetSettings : CommandSettings
    {
        [CommandArgument(0, "<NEW_SERVER>")]
        public Uri NewServer { get; init; }
    }

    internal sealed class ServerShowSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }
    }

    internal sealed class AccountNewSettings : CommandSettings
    {
        [CommandArgument(0, "<email>")]
        public string Email { get; init; }

        [CommandOption("--out-path|--out <OUT_PATH>")]
        public string OutPath { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class AccountSetSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandArgument(0, "<key-path>")]
        public string KeyPath { get; init; }
    }

    internal sealed class AccountShowSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class AccountUpdateSettings : CommandSettings
    {
        [CommandArgument(0, "<email>")]
        public string Email { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class OrderNewSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }

        [CommandArgument(0, "<domains>")]
        public string[] Domains { get; init; }

        [CommandOption("--profile <PROFILE>")]
        [Description("Certificate profile advertised by the server, for example 'shortlived'.")]
        public string Profile { get; init; }

        [CommandOption("--replaces <CERTIFICATE_ID>")]
        [Description("ARI certificate ID of the certificate this order replaces; see 'cert renewal-info'.")]
        public string Replaces { get; init; }

        // Reject supplied-but-empty values (for example an unset shell variable) instead of
        // silently creating an order without the requested profile or replacement.
        public override ValidationResult Validate()
            => Profile != null && string.IsNullOrWhiteSpace(Profile)
                ? ValidationResult.Error("--profile requires a non-empty value.")
                : Replaces != null && string.IsNullOrWhiteSpace(Replaces)
                    ? ValidationResult.Error("--replaces requires a non-empty value.")
                    : ValidationResult.Success();
    }

    internal sealed class OrderListSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class OrderShowSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }

        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }
    }

    internal sealed class OrderAuthzSettings : CommandSettings
    {
        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandArgument(1, "<domain>")]
        public string Domain { get; init; }

        [CommandArgument(2, "<challenge-type>")]
        public string ChallengeType { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class OrderValidateSettings : CommandSettings
    {
        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandArgument(1, "<domain>")]
        public string Domain { get; init; }

        [CommandArgument(2, "<challenge-type>")]
        public string ChallengeType { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class OrderFinalizeSettings : CommandSettings
    {
        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }

        [CommandOption("--dn <DN>")]
        public string Dn { get; init; }

        [CommandOption("--out-path|--out <OUT_PATH>")]
        public string OutPath { get; init; }

        [CommandOption("--private-key <PRIVATE_KEY>")]
        public string PrivateKey { get; init; }

        [CommandOption("--key-algorithm <KEY_ALGORITHM>")]
        public KeyAlgorithm KeyAlgorithm { get; init; } = KeyAlgorithm.ES256;
    }

    internal sealed class CertificatePemSettings : CommandSettings
    {
        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandOption("--preferred-chain <PREFERRED_CHAIN>")]
        public string PreferredChain { get; init; }

        [CommandOption("--out-path|--out <OUT_PATH>")]
        public string OutPath { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class CertificateRenewalInfoSettings : CommandSettings
    {
        [CommandArgument(0, "<cert-path>")]
        [Description("Path to the certificate PEM; the first certificate is used.")]
        public string CertPath { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }
    }

    internal sealed class CertificatePfxSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }

        [CommandOption("--out-path|--out <OUT_PATH>")]
        public string OutPath { get; init; }

        [CommandOption("--private-key <PRIVATE_KEY>")]
        public string PrivateKey { get; init; }

        [CommandOption("--friendly-name <FRIENDLY_NAME>")]
        public string FriendlyName { get; init; }

        [CommandOption("--issuer <ISSUER>")]
        public string Issuer { get; init; }

        [CommandOption("--preferred-chain <PREFERRED_CHAIN>")]
        public string PreferredChain { get; init; }

        [CommandOption("--legacy-encryption")]
        [Description("Use 3DES/RC2 instead of AES-256, for consumers such as Windows Server 2016 and earlier that cannot read AES-encrypted PFX files.")]
        public bool LegacyEncryption { get; init; }

        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandArgument(1, "<password>")]
        public string Password { get; init; }
    }

}
