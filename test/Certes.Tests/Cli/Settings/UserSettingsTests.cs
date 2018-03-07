using System;
using System.IO;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Cli.Settings;
using Certes.Json;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Certes.Cli
{
    public class UserSettingsTests
    {
        [Fact]
        public async Task CanSetServer()
        {
            var uri = new Uri("http://acme.d/d");
            var fullPath = Path.GetFullPath($"./{nameof(CanSetServer)}");
            var configPath = Path.Combine(fullPath, ".certes", "certes.json");
            var envMock = GetEnvMock(fullPath);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.ReadAllText(It.IsAny<string>())).ReturnsAsync((string)null);
            fileMock.Setup(m => m.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var settings = new UserSettings(fileMock.Object, envMock.Object);
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
            var envMock = GetEnvMock(fullPath);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.ReadAllText(It.IsAny<string>())).ReturnsAsync((string)null);

            var settings = new UserSettings(fileMock.Object, envMock.Object);
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
            var envMock = GetEnvMock(fullPath, false);

            var model = new UserSettings.Model { Azure = new AzureSettings { SubscriptionId = Guid.NewGuid().ToString("N") } };
            var json = JsonConvert.SerializeObject(model, JsonUtil.CreateSettings());
            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.ReadAllText(It.IsAny<string>())).ReturnsAsync(json);

            var settings = new UserSettings(fileMock.Object, envMock.Object);
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
        public async Task CanSetAzureSettings()
        {
            var fullPath = Path.GetFullPath($"./{nameof(CanSetAzureSettings)}");
            var configPath = Path.Combine(fullPath, ".certes", "certes.json");
            var envMock = GetEnvMock(fullPath);

            var azSettings = new AzureSettings
            {
                ClientId = Guid.NewGuid().ToString(),
                ClientSecret = Guid.NewGuid().ToString(),
                SubscriptionId = Guid.NewGuid().ToString(),
                TalentId = Guid.NewGuid().ToString(),
            };

            var json = JsonConvert.SerializeObject(new UserSettings.Model { Azure = azSettings }, JsonUtil.CreateSettings());
            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.ReadAllText(It.IsAny<string>())).ReturnsAsync((string)null);
            fileMock.Setup(m => m.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var settings = new UserSettings(fileMock.Object, envMock.Object);
            await settings.SetAzureSettings(azSettings);

            fileMock.Verify(m => m.WriteAllText(configPath, json), Times.Once);
        }

        private static Mock<IEnvironmentVariables> GetEnvMock(string path, bool forWin = true)
        {
            var mock = new Mock<IEnvironmentVariables>(MockBehavior.Strict);
            path = Path.GetFullPath(path);
            if (forWin)
            {
                var drive = Path.GetPathRoot(path);
                mock.Setup(m => m.GetVar("HOME")).Returns("");
                mock.Setup(m => m.GetVar("HOMEDRIVE")).Returns(drive);
                mock.Setup(m => m.GetVar("HOMEPATH")).Returns(path.Substring(drive.Length));
            }
            else
            {
                mock.Setup(m => m.GetVar("HOME")).Returns(path);
                mock.Setup(m => m.GetVar("HOMEDRIVE")).Returns("");
                mock.Setup(m => m.GetVar("HOMEPATH")).Returns("");
            }

            return mock;
        }
    }
}
