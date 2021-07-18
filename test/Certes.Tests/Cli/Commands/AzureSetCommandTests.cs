using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Rest.Azure;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using static Certes.Cli.CliTestHelper;

namespace Certes.Cli.Commands
{
    public class AzureSetCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var resourceGroups = new[] 
            {
                new ResourceGroupInner(location: "/resourceGroups/group1", name: "group1"),
                new ResourceGroupInner(location: "/resourceGroups/group2", name: "group2"),
            };

            var azSettings = new AzureSettings
            {
                ClientId = "clientId",
                ClientSecret = "secret",
                SubscriptionId = Guid.NewGuid().ToString("N"),
                TenantId = Guid.NewGuid().ToString("N"),
            };

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetAzureSettings()).ReturnsAsync(new AzureSettings());
            settingsMock.Setup(m => m.SetAzureSettings(It.IsAny<AzureSettings>())).Returns(Task.CompletedTask);

            var resMock = new Mock<IResourceManagementClient>(MockBehavior.Strict);
            var resGroupOpMock = new Mock<IResourceGroupsOperations>(MockBehavior.Strict);
            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);

            resMock.Setup(m => m.Dispose());
            resMock.SetupSet(m => m.SubscriptionId);
            resMock.SetupGet(m => m.ResourceGroups).Returns(resGroupOpMock.Object);
            resGroupOpMock.Setup(m => m.ListWithHttpMessagesAsync(default, default, default))
                .ReturnsAsync(new AzureOperationResponse<IPage<ResourceGroupInner>>
                {
                    Body = JsonConvert.DeserializeObject<Page<ResourceGroupInner>>(
                        JsonConvert.SerializeObject(new
                        {
                            value = resourceGroups
                        })
                    )
                });

            var (console, stdOutput, errOutput) = MockConsole();

            var cmd = new AzureSetCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object, _ => resMock.Object);
            var command = cmd.Define();

            var args =
                $"set" +
                $" --tenant-id {azSettings.TenantId} --client-id {azSettings.ClientId}" +
                $" --client-secret {azSettings.ClientSecret}" +
                $" --subscription-id {azSettings.SubscriptionId}";
            await command.InvokeAsync(args, console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            dynamic ret = JsonConvert.DeserializeObject(stdOutput.ToString());

            var resourceGroupsReturned = ret.resourceGroups as JArray;
            Assert.Equal(2, resourceGroupsReturned.Count);
            settingsMock.Verify(m => m.SetAzureSettings(It.IsAny<AzureSettings>()), Times.Once);
        }
    }
}
