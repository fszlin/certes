#if NETCOREAPP2_0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace Certes.Cli
{
    public class CliTests
    {
        // TODO: Setup boulder for testing
        private const string Host = "certes-ci.dymetis.com";
        private const string OutputPrefix = "./_test/cli-example";
        private readonly string AccountPath = $"{OutputPrefix}/context.json";
        private string cmd;

        [Fact]
        public async Task RunStaging()
        {
            var server = await Helper.GetStagingServer();

            // Create new Registration
            cmd = $"register --register-unsafely-without-email --agree-tos --server {server} --path {AccountPath} --force";
            await RunCommand(cmd);

            InjectTestKey();

            // Update registration to accept terms of services
            cmd = $"register --agree-tos --update-registration --server {server} --path {AccountPath}";
            await RunCommand(cmd);

            // Initialize authorization
            cmd = $"authz --value {Host} --type dns --server {server} --path {AccountPath}";
            await RunCommand(cmd);

            // Comptue key authorization for http-01
            cmd = $"authz --value {Host} --type dns --key-authz http-01 --server {server} --path {AccountPath}";
            await RunCommand(cmd);

            // Submit key authorization for http-01
            cmd = $"authz --value {Host} --type dns --complete-authz http-01 --server {server} --path {AccountPath}";
            await RunCommand(cmd);

            // Refresh key authorization for http-01
            cmd = $"authz --value {Host} --type dns --refresh http-01 --server {server} --path {AccountPath}";
            await RunCommand(cmd);

            while (IsAuthPending())
            {
                await Task.Delay(5000);
                await RunCommand(cmd);
            }

            // Create a certificate with friendly name
            cmd = $"cert --value {Host} --name mycert --distinguished-name %dn% --server {server} --path {AccountPath}";
            await RunCommand(cmd, new Dictionary<string, string>
            {
                { "%dn%", $"CN=CA, ST=Ontario, L=Toronto, O=Certes, OU=Dev, CN={Host}" }
            });

            // Export certificate
            cmd = $"cert --name mycert --export-cer {OutputPrefix}/mycert.cer --path {AccountPath}";
            await RunCommand(cmd);

            // Export private key
            cmd = $"cert --name mycert --export-key {OutputPrefix}/mycert.key --path {AccountPath}";
            await RunCommand(cmd);

            // Export pfx
            cmd = $"cert --name mycert --export-pfx {OutputPrefix}/mycert.pfx --password abcd1234 --path {AccountPath} --full-chain-off";
            await RunCommand(cmd);

            // Revoke certificate
            cmd = $"cert --name mycert --revoke --server {server} --path {AccountPath}";
            await RunCommand(cmd);
        }

        private bool IsAuthPending()
        {
            var json = File.ReadAllText(AccountPath);
            var ctx = JObject.Parse(json);
            return ctx["authorizations"]["dns"][Host]["data"].Value<string>("status") == EntityStatus.Pending;
        }

        private async Task<IList<string>> RunCommand(string cmd, Dictionary<string, string> placeHolders = null)
        {
            var config = new LoggingConfiguration();
            var memoryTarget = new MemoryTarget()
            {
                Layout = @"${message}${onexception:${exception:format=tostring}}"
            };

            config.AddTarget("logger", memoryTarget);

            var consoleRule = new LoggingRule("*", LogLevel.Debug, memoryTarget);
            config.LoggingRules.Add(consoleRule);
            
            var args = cmd.Split(' ')
                .Select(s => placeHolders?.ContainsKey(s) == true ? placeHolders[s] : s)
                .ToArray();

            using (var factory = new LogFactory(config))
            {
                var logger = factory.GetLogger("logger");
                var succeed = await new Program(logger).Process(args);
                Assert.True(succeed, string.Join(Environment.NewLine, memoryTarget.Logs));
            }

            return memoryTarget.Logs;
        }

        private void InjectTestKey()
        {
            var json = File.ReadAllText(AccountPath);
            var ctx = JObject.Parse(json);
            ctx["account"]["key"]["der"] = Helper.PrivateKey;
            File.WriteAllText(AccountPath, ctx.ToString(Formatting.Indented));
        }
    }
}

#endif
