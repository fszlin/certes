using Certes.Acme;
using Certes.Cli.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Cli.Processors
{
    internal class AuthorizationCommand : CommandBase<AuthorizationOptions>
    {
        private static readonly char[] NameValueSeparator = new[] { '\r', '\n', ' ', ';', ',' };
        public AuthorizationCommand(AuthorizationOptions options)
            : base(options)
        {
        }

        public override async Task<AcmeContext> Process(AcmeContext context)
        {
            if (context?.Account == null)
            {
                throw new Exception("Account not specified.");
            }

            var values = await GetAllValues();

            if (values.Length == 0)
            {
                throw new Exception("Value not specified.");
            }

            if (!string.IsNullOrWhiteSpace(Options.Complete))
            {
                await CompleteChallenge(context, values);
            }
            else if (!string.IsNullOrWhiteSpace(Options.KeyAuthentication))
            {
                ComputeKeyAuthorization(context, values);
            }
            else if (!string.IsNullOrWhiteSpace(Options.Refresh))
            {
                await RefreshAuthorization(context, values);
            }
            else
            {
                await NewAuthorization(context, values);
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
                values = values.Union(names).Distinct().ToArray();
            }

            return values;
        }

        private async Task NewAuthorization(AcmeContext context, string[] values)
        {
            if (context.Authorizations == null)
            {
                context.Authorizations = new Dictionary<string, IDictionary<string, AcmeResult<Authorization>>>();
            }

            var authorizations = context.Authorizations[Options.Type] =
                context.Authorizations.TryGet(Options.Type) ?? new Dictionary<string, AcmeResult<Authorization>>();
            using (var client = new AcmeClient(Options.Server))
            {
                client.Use(context.Account.Key);

                foreach (var name in values)
                {
                    var id = new AuthorizationIdentifier()
                    {
                        Type = Options.Type,
                        Value = name
                    };

                    try
                    {
                        var auth = await client.NewAuthorization(id);
                        Console.WriteLine("{0} {1}", name, auth.Data.Status);

                        authorizations[auth.Data.Identifier.Value] = auth;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0} Error - {1}", name, ex.Message);
                    }
                }
            }
        }

        private void ComputeKeyAuthorization(AcmeContext context, string[] values)
        {
            var authorizations = context.Authorizations?.TryGet(Options.Type);
            using (var client = new AcmeClient(Options.Server))
            {
                client.Use(context.Account.Key);

                foreach (var name in values)
                {
                    var auth = authorizations?.TryGet(name);

                    var challenge = auth?
                        .Data?
                        .Challenges?
                        .Where(c => c.Type == Options.KeyAuthentication)
                        .FirstOrDefault();

                    if (challenge == null)
                    {
                        Console.WriteLine("{0} NotFound", name);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(challenge.KeyAuthorization) || Options.Force)
                        {
                            challenge.KeyAuthorization = client.ComputeKeyAuthorization(challenge);
                        }

                        Console.WriteLine("{0} {1}", name, challenge.KeyAuthorization);
                    }
                }
            }
        }

        private async Task RefreshAuthorization(AcmeContext context, string[] values)
        {
            var authorizations = context.Authorizations?.TryGet(Options.Type);
            using (var client = new AcmeClient(Options.Server))
            {
                client.Use(context.Account.Key);

                foreach (var name in values)
                {
                    var auth = authorizations?.TryGet(name);
                    if (auth != null)
                    {
                        auth = authorizations[name] = await client.RefreshAuthorization(auth.Location);

                        var challenge = auth?
                            .Data?
                            .Challenges?
                            .Where(c => c.Type == Options.Refresh)
                            .FirstOrDefault();

                        Console.WriteLine("{0} {1}", name, challenge.Status);
                    }
                }
            }
        }

        private async Task CompleteChallenge(AcmeContext context, string[] values)
        {
            var authorizations = context.Authorizations?.TryGet(Options.Type);
            using (var client = new AcmeClient(Options.Server))
            {
                client.Use(context.Account.Key);

                foreach (var name in values)
                {
                    var auth = authorizations?.TryGet(name);

                    var challenge = auth
                        .Data?
                        .Challenges?
                        .Where(c => c.Type == Options.Complete)
                        .FirstOrDefault();

                    if (challenge == null)
                    {
                        Console.WriteLine("{0} NotFound", name);
                    }
                    else
                    {
                        var challengeResult = await client.CompleteChallenge(challenge);
                        challenge = challengeResult.Data;

                        auth.Data.Challenges = auth.Data.Challenges
                            .Where(c => c.Type != challenge.Type)
                            .Union(new[] { challenge })
                            .ToArray();
                        Console.WriteLine("{0} {1}", name, challenge.Status);
                    }
                }
            }
        }
    }

}
