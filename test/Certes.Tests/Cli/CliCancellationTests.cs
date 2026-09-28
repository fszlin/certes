using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using Moq;
using Xunit;
using Directory = Certes.Acme.Resource.Directory;

namespace Certes.Cli
{
    public class CliCancellationTests
    {
        private static readonly Uri Server = new Uri("https://example.test/directory");
        private static readonly Uri Location = new Uri("https://example.test/order/1");

        [Fact]
        public void SecondInterruptAllowsDefaultTermination()
        {
            using var cancellation = new CancellationTokenSource();
            var count = 0;
            Assert.True(Program.CancelInvocation(cancellation, ref count));
            Assert.True(cancellation.IsCancellationRequested);
            Assert.False(Program.CancelInvocation(cancellation, ref count));
        }

        [Theory]
        [InlineData("server")]
        [InlineData("order")]
        [InlineData("account")]
        [InlineData("file")]
        [InlineData("settings")]
        public async Task CancellationReachesPendingCommandWork(string operation)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<T> Block<T>()
            {
                entered.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, token);
                throw new InvalidOperationException();
            }
            var settings = new Mock<IUserSettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetDefaultServer(token)).ReturnsAsync(Server);
            settings.Setup(s => s.GetAccountKey(Server, token)).ReturnsAsync(Helper.GetKeyV2());
            if (operation == "settings") settings.Setup(s => s.GetDefaultServer(token)).Returns(() => Block<Uri>());
            var acme = new Mock<IAcmeContext>(MockBehavior.Strict);
            acme.Setup(c => c.GetDirectory(token)).Returns(() => Block<Directory>());
            acme.Setup(c => c.Account(token)).Returns(() => Block<IAccountContext>());
            var order = new Mock<IOrderContext>(MockBehavior.Strict);
            order.SetupGet(o => o.Location).Returns(Location);
            order.Setup(o => o.Resource(token)).Returns(() => Block<Order>());
            acme.Setup(c => c.Order(Location)).Returns(order.Object);
            var files = new Mock<IFileUtil>(MockBehavior.Strict);
            files.Setup(f => f.ReadAllText("account.pem", token)).Returns(() => Block<string>());
            var cli = Create(acme, settings, files);
            var args = operation switch
            {
                "server" or "settings" => new[] { "server", "show" },
                "order" => new[] { "order", "show", Location.ToString() },
                "file" => new[] { "account", "show", "--key", "account.pem" },
                _ => new[] { "account", "show" },
            };
            var pending = cli.RunWithExitCode(args, token);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();
            Assert.Equal(130, await pending.WaitAsync(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public async Task PreCancelledInvocationDoesNotAccessDependencies()
        {
            var cli = Create(new Mock<IAcmeContext>(MockBehavior.Strict));
            Assert.Equal(130, await cli.RunWithExitCode(new[] { "server", "show" }, new CancellationToken(true)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UnrelatedCancellationAndNetworkErrorsRemainFailures(bool cancelled)
        {
            var acme = new Mock<IAcmeContext>(MockBehavior.Strict);
            acme.Setup(c => c.GetDirectory(default)).ThrowsAsync(cancelled
                ? new OperationCanceledException() : new System.Net.Http.HttpRequestException("offline"));
            Assert.Equal(1, await Create(acme).RunWithExitCode(new[] { "server", "show", "--server", Server.ToString() }));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SuccessfulAccountCreationPersistsKeyAfterLateCancellation(bool toFile)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var account = new Mock<IAccountContext>(MockBehavior.Strict);
            account.SetupGet(a => a.Location).Returns(Location);
            var acme = new Mock<IAcmeContext>(MockBehavior.Strict);
            acme.Setup(c => c.NewAccount(It.IsAny<IList<string>>(), true, null, null, null, token))
                .Callback(() => cancellation.Cancel()).ReturnsAsync(account.Object);
            var settings = new Mock<IUserSettings>(MockBehavior.Strict);
            var files = new Mock<IFileUtil>(MockBehavior.Strict);
            if (toFile)
            {
                files.Setup(f => f.WriteAllText("new.pem", It.IsAny<string>(), token)).Returns(Task.CompletedTask);
            }
            else
            {
                settings.Setup(s => s.SetAccountKey(Server, It.IsAny<IKey>(), CancellationToken.None)).Returns(Task.CompletedTask);
            }
            var args = new List<string> { "account", "new", "test@example.test", "--server", Server.ToString() };
            if (toFile) args.AddRange(new[] { "--out", "new.pem" });
            Assert.Equal(130, await Create(acme, settings, files).RunWithExitCode(args.ToArray(), token));
            if (toFile) files.Verify(f => f.WriteAllText("new.pem", It.IsAny<string>(), token), Times.Once);
            else settings.Verify(s => s.SetAccountKey(Server, It.IsAny<IKey>(), CancellationToken.None), Times.Once);
            account.Verify(a => a.Resource(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SuccessfulFinalizeSavesGeneratedKeyAndReturnsSuccessAfterLateCancellation()
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var settings = new Mock<IUserSettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetAccountKey(Server, token)).ReturnsAsync(Helper.GetKeyV2());
            var order = new Mock<IOrderContext>(MockBehavior.Strict);
            order.SetupGet(o => o.Location).Returns(Location);
            order.Setup(o => o.Resource(token)).ReturnsAsync(new Order
            {
                Identifiers = new[] { new Identifier { Type = IdentifierType.Dns, Value = "example.test" } },
            });
            order.Setup(o => o.Finalize(It.IsAny<byte[]>(), token)).Callback(() => cancellation.Cancel())
                .ReturnsAsync(new Order { Status = OrderStatus.Processing });
            var acme = new Mock<IAcmeContext>(MockBehavior.Strict);
            acme.Setup(c => c.Order(Location)).Returns(order.Object);
            var files = new Mock<IFileUtil>(MockBehavior.Strict);
            files.Setup(f => f.WriteAllText("cert.pem", It.IsAny<string>(), token)).Returns(Task.CompletedTask);
            var env = new Mock<IEnvironmentVariables>(MockBehavior.Strict);
            env.Setup(e => e.GetVar("CERTES_CERT_KEY")).Returns((string)null);
            var cli = new CliCoreSpectre(settings.Object, (_, _) => acme.Object, files.Object, env.Object);
            Assert.Equal(0, await cli.RunWithExitCode(new[] { "order", "finalize", Location.ToString(), "--server", Server.ToString(), "--out", "cert.pem" }, token));
            files.Verify(f => f.WriteAllText("cert.pem", It.IsAny<string>(), token), Times.Once);
        }

        [Fact]
        public async Task FileCancellationLeavesDestinationUntouched()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            try
            {
                await File.WriteAllTextAsync(path, "original");
                var files = new FileUtil();
                var token = new CancellationToken(true);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => files.WriteAllText(path, "replacement", token));
                Assert.Equal("original", await File.ReadAllTextAsync(path));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => files.ReadAllText(path, token));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task UserSettingsForwardsCancellationToFileRead()
        {
            using var cancellation = new CancellationTokenSource();
            var files = new Mock<IFileUtil>(MockBehavior.Strict);
            files.Setup(f => f.ReadAllText(It.IsAny<string>(), cancellation.Token))
                .ThrowsAsync(new OperationCanceledException(cancellation.Token));
            var env = new Mock<IEnvironmentVariables>();
            env.Setup(e => e.GetVar("HOME")).Returns("/home/test");
            var settings = new UserSettings(files.Object, env.Object);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => settings.GetDefaultServer(cancellation.Token));
            files.Verify(f => f.ReadAllText(It.IsAny<string>(), cancellation.Token), Times.Once);
        }

        [Theory]
        [InlineData("validate")]
        [InlineData("download")]
        public async Task CancellationReachesChallengeAndCertificateOperations(string operation)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var settings = new Mock<IUserSettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetDefaultServer(token)).ReturnsAsync(Server);
            settings.Setup(s => s.GetAccountKey(Server, token)).ReturnsAsync(Helper.GetKeyV2());
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<T> Block<T>()
            {
                entered.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, token);
                throw new InvalidOperationException();
            }
            var order = new Mock<IOrderContext>(MockBehavior.Strict);
            var authz = new Mock<IAuthorizationContext>(MockBehavior.Strict);
            authz.Setup(a => a.Resource(token)).ReturnsAsync(new Authorization
            {
                Identifier = new Identifier { Type = IdentifierType.Dns, Value = "example.test" },
            });
            var challenge = new Mock<IChallengeContext>(MockBehavior.Strict);
            challenge.SetupGet(c => c.Type).Returns(ChallengeTypes.Http01);
            challenge.SetupGet(c => c.Location).Returns(Location);
            challenge.Setup(c => c.Validate(token)).Returns(() => Block<Challenge>());
            authz.Setup(a => a.Challenges(token)).ReturnsAsync(new[] { challenge.Object });
            order.Setup(o => o.Authorizations(token)).ReturnsAsync(new[] { authz.Object });
            order.Setup(o => o.Resource(token)).ReturnsAsync(new Order { Status = OrderStatus.Valid, Certificate = Location });
            order.Setup(o => o.Download(null, token)).Returns(() => Block<CertificateChain>());
            var acme = new Mock<IAcmeContext>(MockBehavior.Strict);
            acme.Setup(c => c.Order(Location)).Returns(order.Object);
            var args = operation == "validate"
                ? new[] { "order", "validate", Location.ToString(), "example.test", "http" }
                : new[] { "cert", "pem", Location.ToString() };
            var pending = Create(acme, settings).RunWithExitCode(args, token);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();
            Assert.Equal(130, await pending.WaitAsync(TimeSpan.FromSeconds(5)));
        }

        private static CliCoreSpectre Create(Mock<IAcmeContext> acme, Mock<IUserSettings> settings = null, Mock<IFileUtil> files = null)
            => new CliCoreSpectre((settings ?? new Mock<IUserSettings>(MockBehavior.Strict)).Object,
                (_, _) => acme.Object, (files ?? new Mock<IFileUtil>(MockBehavior.Strict)).Object,
                new Mock<IEnvironmentVariables>(MockBehavior.Strict).Object);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExplicitOutputKeySurvivesInFlightCancellation(bool finalize)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pem");
            var settings = new Mock<IUserSettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetAccountKey(Server, token)).ReturnsAsync(Helper.GetKeyV2());
            var env = new Mock<IEnvironmentVariables>();
            var acme = new Mock<IAcmeContext>(MockBehavior.Strict);
            IKey accountKey = null;
            string savedPem = null;
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<T> InFlight<T>()
            {
                savedPem = await File.ReadAllTextAsync(path);
                Assert.NotNull(KeyFactory.FromPem(savedPem));
                entered.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, token);
                throw new InvalidOperationException();
            }
            var order = new Mock<IOrderContext>(MockBehavior.Strict);
            order.Setup(o => o.Resource(token)).ReturnsAsync(new Order
            {
                Identifiers = new[] { new Identifier { Type = IdentifierType.Dns, Value = "example.test" } },
            });
            order.Setup(o => o.Finalize(It.IsAny<byte[]>(), token)).Returns(() => InFlight<Order>());
            acme.Setup(c => c.Order(Location)).Returns(order.Object);
            acme.Setup(c => c.NewAccount(It.IsAny<IList<string>>(), true, null, null, null, token))
                .Returns(() => InFlight<IAccountContext>());
            var cli = new CliCoreSpectre(settings.Object, (_, key) => { accountKey = key; return acme.Object; }, new FileUtil(), env.Object);
            var args = finalize
                ? new[] { "order", "finalize", Location.ToString(), "--server", Server.ToString(), "--out", path }
                : new[] { "account", "new", "test@example.test", "--server", Server.ToString(), "--out", path };
            try
            {
                var pending = cli.RunWithExitCode(args, token);
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
                cancellation.Cancel();
                Assert.Equal(130, await pending.WaitAsync(TimeSpan.FromSeconds(5)));
                Assert.Equal(savedPem, await File.ReadAllTextAsync(path));
                if (!finalize) Assert.Equal(accountKey.Thumbprint(), KeyFactory.FromPem(savedPem).Thumbprint());
                if (!OperatingSystem.IsWindows())
                {
                    Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
                }
            }
            finally
            {
                cancellation.Cancel();
                File.Delete(path);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task FailedOrCancelledKeyPersistencePreventsRequest(bool finalize, bool cancelled)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var settings = new Mock<IUserSettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetAccountKey(Server, token)).ReturnsAsync(Helper.GetKeyV2());
            var order = new Mock<IOrderContext>(MockBehavior.Strict);
            order.Setup(o => o.Resource(token)).ReturnsAsync(new Order
            {
                Identifiers = new[] { new Identifier { Type = IdentifierType.Dns, Value = "example.test" } },
            });
            var acme = new Mock<IAcmeContext>(MockBehavior.Strict);
            acme.Setup(c => c.Order(Location)).Returns(order.Object);
            var files = new Mock<IFileUtil>(MockBehavior.Strict);
            files.Setup(f => f.WriteAllText("key.pem", It.IsAny<string>(), token)).Returns(() =>
            {
                if (!cancelled) throw new IOException("Cannot save key");
                cancellation.Cancel();
                // Also verifies the check after a completed save, before sending.
                return Task.CompletedTask;
            });
            var env = new Mock<IEnvironmentVariables>();
            var cli = new CliCoreSpectre(settings.Object, (_, _) => acme.Object, files.Object, env.Object);
            var args = finalize
                ? new[] { "order", "finalize", Location.ToString(), "--server", Server.ToString(), "--out", "key.pem" }
                : new[] { "account", "new", "test@example.test", "--server", Server.ToString(), "--out", "key.pem" };
            Assert.Equal(cancelled ? 130 : 1, await cli.RunWithExitCode(args, token));
            files.Verify(f => f.WriteAllText("key.pem", It.IsAny<string>(), token), Times.Once);
            acme.Verify(c => c.NewAccount(It.IsAny<IList<string>>(), true, null, null, null, token), Times.Never);
            order.Verify(o => o.Finalize(It.IsAny<byte[]>(), token), Times.Never);
        }
    }
}
