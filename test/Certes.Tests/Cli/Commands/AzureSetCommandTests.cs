using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Rest.Azure;
using Moq;
using Newtonsoft.Json;
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
                TalentId = Guid.NewGuid().ToString("N"),
            };

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetAzureSettings()).ReturnsAsync(new AzureSettings());
            settingsMock.Setup(m => m.SetAzureSettings(It.IsAny<AzureSettings>())).Returns(Task.CompletedTask);

            var resMock = new Mock<IResourceManagementClient>(MockBehavior.Strict);
            var resGroupOpMock = new Mock<IResourceGroupsOperations>(MockBehavior.Strict);

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
                
            var cmd = new AzureSetCommand(
                settingsMock.Object, MakeFactory(resMock));

            var syntax = DefineCommand(
                $"set" +
                $" --talent-id {azSettings.TalentId} --client-id {azSettings.ClientId}" +
                $" --client-secret {azSettings.ClientSecret}" +
                $" --subscription-id {azSettings.SubscriptionId}");
            dynamic ret = await cmd.Execute(syntax);

            Assert.Equal(2, ret.resourceGroups.Length);
            settingsMock.Verify(m => m.SetAzureSettings(It.IsAny<AzureSettings>()), Times.Once);
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"set"
                + " --talent-id talentId --client-id clientId --client-secret abcd1234"
                + " --subscription-id subscriptionId";
            var syntax = DefineCommand(args);

            Assert.Equal("set", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "talent-id", "talentId");
            ValidateOption(syntax, "client-id", "clientId");
            ValidateOption(syntax, "client-secret", "abcd1234");
            ValidateOption(syntax, "subscription-id", "subscriptionId");

            syntax = DefineCommand("noop");
            Assert.NotEqual("set", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new AzureSetCommand(
                new UserSettings(new FileUtil()), null);
            Assert.Equal(CommandGroup.Azure.Command, cmd.Group.Command);
            return ArgumentSyntax.Parse(args.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                cmd.Define(syntax);
            });
        }
    }
}
