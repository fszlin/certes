using System;
using System.IO;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Cli.Options;
using Certes.Cli.Settings;
using Certes.Json;
using Moq;
using Newtonsoft.Json;
using Xunit;

using static Certes.Helper;

namespace Certes.Cli
{
    [Collection(nameof(ContextFactory))]
    public class UserSettingsTests
    {
        [Fact]
        public async Task CanSetServer()
        {
            var uri = new Uri("http://acme.d/d");
            var fullPath = Path.GetFullPath($"./{nameof(CanSetServer)}");
            var configPath = Path.Combine(fullPath, ".certes", "certes.json");
            SetHomePath(fullPath);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.ReadAllText(It.IsAny<string>())).ReturnsAsync((string)null);
            fileMock.Setup(m => m.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var settings = new UserSettings(fileMock.Object);
            await settings.SetDefaultServer(uri);
            fileMock.Verify(m => m.ReadAllText(configPath), Times.Once);

            var json = JsonConvert.SerializeObject(new UserSettings.Model { DefaultServer = uri }, JsonUtil.CreateSettings());
            fileMock.Verify(m => m.WriteAllText(configPath, json), Times.Once);

            fileMock.ResetCalls();
            var model = new UserSettings.Model { DefaultServer = uri, Servers = new AcmeSettings[0] };
            fileMock.Setup(m => m.ReadAllText(It.IsAny<string>()))
                .ReturnsAsync(JsonConvert.SerializeObject(model, JsonUtil.CreateSettings()));

            await settings.SetDefaultServer(uri);

            fileMock.Verify(m => m.ReadAllText(configPath), Times.Once);
            model.DefaultServer = uri;
            json = JsonConvert.SerializeObject(model, JsonUtil.CreateSettings());
            fileMock.Verify(m => m.WriteAllText(configPath, json), Times.Once);
        }

        [Fact]
        public async Task CanGetServer()
        {
            var uri = new Uri("http://acme.d/d");
            var fullPath = Path.GetFullPath($"./{nameof(CanGetServer)}");
            var configPath = Path.Combine(fullPath, ".certes", "certes.json");
            SetHomePath(fullPath);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.ReadAllText(It.IsAny<string>())).ReturnsAsync((string)null);

            var settings = new UserSettings(fileMock.Object);
            Assert.Equal(WellKnownServers.LetsEncryptV2, await settings.GetDefaultServer());
            fileMock.Verify(m => m.ReadAllText(configPath), Times.Once);

            fileMock.ResetCalls();
            var model = new UserSettings.Model { DefaultServer = uri, Servers = new AcmeSettings[0] };
            fileMock.Setup(m => m.ReadAllText(It.IsAny<string>()))
                .ReturnsAsync(JsonConvert.SerializeObject(model, JsonUtil.CreateSettings()));

            Assert.Equal(uri, await settings.GetDefaultServer());

            fileMock.Verify(m => m.ReadAllText(configPath), Times.Once);
        }

        [Fact]
        public async Task CanGetAzureSettings()
        {
            var fullPath = Path.GetFullPath($"./{nameof(CanGetAzureSettings)}");
            SetHomePath(fullPath, false);

            var model = new UserSettings.Model { Azure = new AzureSettings { SubscriptionId = Guid.NewGuid() } };
            var json = JsonConvert.SerializeObject(model, JsonUtil.CreateSettings());
            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.ReadAllText(It.IsAny<string>())).ReturnsAsync(json);

            var settings = new UserSettings(fileMock.Object);
            var azSettings = await settings.GetAzureSettings();
            Assert.Equal(model.Azure.SubscriptionId, azSettings.SubscriptionId);

            model = new UserSettings.Model();
            json = JsonConvert.SerializeObject(model, JsonUtil.CreateSettings());
            fileMock.Setup(m => m.ReadAllText(It.IsAny<string>())).ReturnsAsync(json);
            azSettings = await settings.GetAzureSettings();
            Assert.NotNull(azSettings);
            Assert.Equal(default, azSettings.SubscriptionId);
        }

        [Fact]
        public async Task CanLoadKeyFromPath()
        {
            var fullPath = Path.GetFullPath($"./{nameof(CanLoadKeyFromPath)}");
            SetHomePath(fullPath);

            File.WriteAllText("./Data/key-es256.pem", Helper.GetTestKey(KeyAlgorithm.ES256));

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
                Path = "./Data/key-es256.pem",
            };

            var userSettings = new UserSettings(new FileUtilImpl());
            var key = await userSettings.GetAccountKey(options, false);

            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task CanLoadKeyFromUnixEvn()
        {
            var fullPath = Path.GetFullPath($"./{nameof(CanLoadKeyFromUnixEvn)}");
            SetHomePath(fullPath, false);

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            var userSettings = new UserSettings(new FileUtilImpl());
            await userSettings.SetAcmeSettings(new AcmeSettings
            {
                ServerUri = WellKnownServers.LetsEncryptV2,
                AccountKey = Helper.GetTestKey(KeyAlgorithm.ES256),
            }, options);

            var key = await userSettings.GetAccountKey(options, false);

            Assert.Equal(Path.Combine(fullPath, ".certes", "certes.json"), userSettings.SettingsFile.Value);
            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task CanLoadKeyFromWinEvn()
        {
            var fullPath = Path.GetFullPath($"./{nameof(CanLoadKeyFromWinEvn)}");
            SetHomePath(fullPath);

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            var userSettings = new UserSettings(new FileUtilImpl());
            await userSettings.SetAcmeSettings(new AcmeSettings
            {
                ServerUri = WellKnownServers.LetsEncryptV2,
                AccountKey = Helper.GetTestKey(KeyAlgorithm.ES256),
            }, options);

            var key = await userSettings.GetAccountKey(options, false);

            Assert.Equal(Path.Combine(fullPath, ".certes", "certes.json"), userSettings.SettingsFile.Value);
            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task NullWhenKeyNotExist()
        {
            var fullPath = Path.GetFullPath($"./{nameof(NullWhenKeyNotExist)}");
            SetHomePath(fullPath);

            var userSettings = new UserSettings(new FileUtilImpl());
            if (Directory.Exists(userSettings.SettingsFile.Value))
            {
                Directory.Delete(userSettings.SettingsFile.Value, true);
            }

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            var key = await userSettings.GetAccountKey(options, false);
            Assert.Null(key);
        }
    }
}
