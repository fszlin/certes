using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Certes.Cli
{
    public class CliRuntimeRelayTests
    {
        [Fact]
        public async Task AccountHelpListsCommandsOnce()
        {
            var result = await RunCli("account", "--help");

            Assert.Equal(0, result.ExitCode);
            var text = StripAnsi(result.StdOut);

            Assert.Equal(1, CountOccurrences(text, "new <email>"));
            Assert.Equal(1, CountOccurrences(text, "set <key-path>"));
            Assert.Equal(1, CountOccurrences(text, "show"));
            Assert.Equal(1, CountOccurrences(text, "update <email>"));
        }

        [Fact]
        public async Task ServerShowHelpIsReachableThroughRelay()
        {
            var result = await RunCli("server", "show", "--help");

            Assert.Equal(0, result.ExitCode);
            var text = StripAnsi(result.StdOut);
            Assert.Contains("USAGE", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("server", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("show", text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AccountShowForwardsServerOption()
        {
            var result = await RunCli("account", "show", "--server", "https://example.invalid");

            Assert.NotEqual(0, result.ExitCode);

            var combined = StripAnsi(result.StdOut + Environment.NewLine + result.StdErr);
            Assert.Contains("https://example.invalid/", combined, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("https://acme-v02.api.letsencrypt.org/directory", combined, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task OrderHelpListsCommandsOnce()
        {
            var result = await RunCli("order", "--help");

            Assert.Equal(0, result.ExitCode);
            var text = StripAnsi(result.StdOut);

            Assert.Equal(1, CountOccurrences(text, "new <domains>"));
            Assert.Equal(1, CountOccurrences(text, "list"));
            Assert.Equal(1, CountOccurrences(text, "show <order-id>"));
            Assert.Equal(1, CountOccurrences(text, "authz <order-id> <domain> <challenge-type>"));
            Assert.Equal(1, CountOccurrences(text, "validate <order-id> <domain> <challenge-type>"));
            Assert.Equal(1, CountOccurrences(text, "finalize <order-id>"));
        }

        [Fact]
        public async Task CertHelpListsCommandsOnce()
        {
            var result = await RunCli("cert", "--help");

            Assert.Equal(0, result.ExitCode);
            var text = StripAnsi(result.StdOut);

            Assert.Equal(1, CountOccurrences(text, "pem <order-id>"));
            Assert.Equal(1, CountOccurrences(text, "pfx <order-id> <password>"));
        }

        [Fact]
        public async Task AzureHelpListsCommandsOnce()
        {
            var result = await RunCli("az", "--help");

            Assert.Equal(0, result.ExitCode);
            var text = StripAnsi(result.StdOut);

            Assert.Equal(1, CountOccurrences(text, "set"));
            Assert.Equal(1, CountOccurrences(text, "dns <order-id> <domain>"));
            Assert.Equal(1, CountOccurrences(text, "app <order-id> <domain> <app>"));
        }

        private static async Task<(int ExitCode, string StdOut, string StdErr)> RunCli(params string[] args)
        {
            var cliDll = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "../../../../../src/Certes.Cli/bin/Debug/net10.0/dotnet-certes.dll"));

            var psi = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            psi.ArgumentList.Add(cliDll);
            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }

            using var process = Process.Start(psi);
            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            var waitTask = process.WaitForExitAsync();
            var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(30)));
            if (completed != waitTask)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("CLI process timed out.");
            }

            return (process.ExitCode, await stdOutTask, await stdErrTask);
        }

        private static string StripAnsi(string input)
            => Regex.Replace(input ?? string.Empty, "\\x1B\\[[0-9;]*[A-Za-z]", string.Empty);

        private static int CountOccurrences(string source, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }
    }
}
