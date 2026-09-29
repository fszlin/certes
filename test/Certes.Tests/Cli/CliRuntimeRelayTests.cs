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
        public async Task CertPfxHelpListsLegacyEncryptionOption()
        {
            var result = await RunCli("cert", "pfx", "--help");

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("--legacy-encryption", StripAnsi(result.StdOut));
        }

        [Fact]
        public async Task UnknownCommandReturnsFailureExitCode()
        {
            var result = await RunCli("unknown-command");
            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Unknown command 'unknown-command'", StripAnsi(result.StdOut + result.StdErr));
        }

        [Fact]
        public async Task CtrlCStopsInFlightRequestOnLinux()
        {
            // Console signal delivery is verified on Linux; cancellation-token
            // dispatch and exit-code tests run on every CLI test platform.
            if (!OperatingSystem.IsLinux()) return;

            using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            var testOutput = new DirectoryInfo(AppContext.BaseDirectory);
            var cliDll = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "../../../../../src/Certes.Cli/bin", testOutput.Parent.Name, testOutput.Name, "dotnet-certes.dll"));
            var start = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var arg in new[] { cliDll, "server", "show", "--server", $"http://127.0.0.1:{port}/directory" })
            {
                start.ArgumentList.Add(arg);
            }
            using var process = Process.Start(start);
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            try
            {
                // The request proves Main registered its handler before sending.
                using var connection = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(10));
                using var signal = Process.Start(new ProcessStartInfo("/bin/kill")
                {
                    ArgumentList = { "-INT", process.Id.ToString() },
                    UseShellExecute = false,
                });
                await signal.WaitForExitAsync();
                Assert.Equal(0, signal.ExitCode);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(130, process.ExitCode);
                Assert.Contains("Operation cancelled.", await stdout + await stderr);
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }
        }

        private static async Task<(int ExitCode, string StdOut, string StdErr)> RunCli(params string[] args)
        {
            // The test output is test/Certes.Tests/bin/<configuration>/<tfm>/; use the
            // CLI built with the same configuration and target framework.
            var testOutput = new DirectoryInfo(AppContext.BaseDirectory);
            var configuration = testOutput.Parent.Name;
            var cliDll = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "../../../../../src/Certes.Cli/bin", configuration, testOutput.Name, "dotnet-certes.dll"));
            Assert.True(File.Exists(cliDll), $"CLI not built at {cliDll}");

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
