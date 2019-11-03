using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Rest.Azure;
using Moq;
using Newtonsoft.Json;
using Xunit;
using static Certes.Acme.WellKnownServers;
using static Certes.Cli.CliTestHelper;
using static Certes.Helper;

namespace Certes.Cli.Commands
{
    public class AzureDnsCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var domain = "www.certes.com";
            var orderLoc = new Uri("http://acme.com/o/1");
            var resourceGroup = "resGroup";

            var challengeLoc = new Uri("http://acme.com/o/1/c/2");
            var authzLoc = new Uri("http://acme.com/o/1/a/1");
            var authz = new Authorization
            {
                Identifier = new Identifier
                {
                    Type = IdentifierType.Dns,
                    Value = domain
                },
                Challenges = new[]
                {
                    new Challenge
                    {
                        Token = "dns-token",
                        Type = ChallengeTypes.Dns01,
                    },
                    new Challenge
                    {
                        Token = "http-token",
                        Type = ChallengeTypes.Http01,
                    },
                }
            };

            var expectedRecordSetId = Guid.NewGuid().ToString();
            var expectedRecordSet = new { data = new RecordSetInner(id: expectedRecordSetId) };

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(LetsEncryptV2);
            settingsMock.Setup(m => m.GetAccountKey(LetsEncryptV2)).ReturnsAsync(GetKeyV2());
            settingsMock.Setup(m => m.GetAzureSettings()).ReturnsAsync(new AzureSettings
            {
                ClientId = "clientId",
                ClientSecret = "secret",
                SubscriptionId = Guid.NewGuid().ToString("N"),
                TenantId = Guid.NewGuid().ToString("N"),
            });

            var challengeMock = new Mock<IChallengeContext>(MockBehavior.Strict);
            challengeMock.SetupGet(m => m.Location).Returns(challengeLoc);
            challengeMock.SetupGet(m => m.Type).Returns(ChallengeTypes.Dns01);
            challengeMock.SetupGet(m => m.Token).Returns(authz.Challenges[0].Token);

            var authzMock = new Mock<IAuthorizationContext>(MockBehavior.Strict);
            authzMock.Setup(m => m.Resource()).ReturnsAsync(authz);
            authzMock.Setup(m => m.Challenges())
                .ReturnsAsync(new[] { challengeMock.Object });

            var orderMock = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock.Setup(m => m.Authorizations()).ReturnsAsync(new[] { authzMock.Object });

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);
            ctxMock.Setup(m => m.Order(orderLoc)).Returns(orderMock.Object);
            ctxMock.SetupGet(m => m.AccountKey).Returns(GetKeyV2());

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);

            var dnsMock = new Mock<IDnsManagementClient>(MockBehavior.Strict);
            var zonesOpMock = new Mock<IZonesOperations>(MockBehavior.Strict);
            var recordSetsOpMock = new Mock<IRecordSetsOperations>(MockBehavior.Strict);

            dnsMock.Setup(m => m.Dispose());
            dnsMock.SetupSet(m => m.SubscriptionId);
            dnsMock.SetupGet(m => m.Zones).Returns(zonesOpMock.Object);
            dnsMock.SetupGet(m => m.RecordSets).Returns(recordSetsOpMock.Object);
            zonesOpMock.Setup(m => m.ListWithHttpMessagesAsync(default, default, default))
                .ReturnsAsync(new AzureOperationResponse<IPage<ZoneInner>>
                {
                    Body = JsonConvert.DeserializeObject<Page<ZoneInner>>(
                        JsonConvert.SerializeObject(new
                        {
                            value = new[] { new ZoneInner(id: "/s/abcd1234/resourceGroups/res/a", name: "certes.com") }
                        })
                    )
                });

            recordSetsOpMock.Setup(m => m.CreateOrUpdateWithHttpMessagesAsync(resourceGroup, "certes.com", "_acme-challenge.www", RecordType.TXT, It.IsAny<RecordSetInner>(), default, default, default, default))
                .ReturnsAsync(new AzureOperationResponse<RecordSetInner>
                {
                    Body = expectedRecordSet.data
                });

            var cmd = new AzureDnsCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object, _ => dnsMock.Object);

            var syntax = DefineCommand(
                $"dns {orderLoc} {domain}" +
                $" --tenant-id tenantId --client-id clientId --client-secret abcd1234" +
                $" --subscription-id {Guid.NewGuid()} --resource-group {resourceGroup}");
            dynamic ret = await cmd.Execute(syntax);
            Assert.Equal(expectedRecordSetId, ret.data.Id);
            recordSetsOpMock.Verify(m => m.CreateOrUpdateWithHttpMessagesAsync(resourceGroup, "certes.com", "_acme-challenge.www", RecordType.TXT, It.IsAny<RecordSetInner>(), default, default, default, default), Times.Once);

            // azure credentials from settings
            recordSetsOpMock.ResetCalls();
            syntax = DefineCommand(
                $"dns {orderLoc} {domain}" +
                $" --resource-group {resourceGroup}");
            ret = await cmd.Execute(syntax);
            Assert.Equal(expectedRecordSetId, ret.data.Id);
            recordSetsOpMock.Verify(m => m.CreateOrUpdateWithHttpMessagesAsync(resourceGroup, "certes.com", "_acme-challenge.www", RecordType.TXT, It.IsAny<RecordSetInner>(), default, default, default, default), Times.Once);

            // wildcard
            recordSetsOpMock.ResetCalls();
            authz.Wildcard = true;
            syntax = DefineCommand(
                $"dns {orderLoc} *.{domain}" +
                $" --tenant-id tenantId --client-id clientId --client-secret abcd1234" +
                $" --subscription-id {Guid.NewGuid()} --resource-group {resourceGroup}");
            ret = await cmd.Execute(syntax);
            Assert.Equal(expectedRecordSetId, ret.data.Id);
            recordSetsOpMock.Verify(m => m.CreateOrUpdateWithHttpMessagesAsync(resourceGroup, "certes.com", "_acme-challenge.www", RecordType.TXT, It.IsAny<RecordSetInner>(), default, default, default, default), Times.Once);
            authz.Wildcard = null;

            // authz not exists
            orderMock.Setup(m => m.Authorizations()).ReturnsAsync(new IAuthorizationContext[0]);
            syntax = DefineCommand(
                $"dns {orderLoc} {domain}" +
                $" --tenant-id tenantId --client-id clientId --client-secret abcd1234" +
                $" --subscription-id {Guid.NewGuid()} --resource-group {resourceGroup}");
            await Assert.ThrowsAsync<CertesCliException>(() => cmd.Execute(syntax));
            orderMock.Setup(m => m.Authorizations()).ReturnsAsync(new[] { authzMock.Object });

            // challenge not exists
            challengeMock.SetupGet(m => m.Type).Returns(ChallengeTypes.Http01);
            syntax = DefineCommand(
                $"dns {orderLoc} {domain}" +
                $" --tenant-id tenantId --client-id clientId --client-secret abcd1234" +
                $" --subscription-id {Guid.NewGuid()} --resource-group {resourceGroup}");
            await Assert.ThrowsAsync<CertesCliException>(() => cmd.Execute(syntax));
            challengeMock.SetupGet(m => m.Type).Returns(ChallengeTypes.Dns01);

            // zone not exists
            zonesOpMock.Setup(m => m.ListWithHttpMessagesAsync(default, default, default))
                .ReturnsAsync(new AzureOperationResponse<IPage<ZoneInner>>
                {
                    Body = JsonConvert.DeserializeObject<Page<ZoneInner>>(
                        JsonConvert.SerializeObject(new
                        {
                            value = new[] { new ZoneInner(id: "/s/abcd1234/resourceGroups/res/a", name: "abc.com") }
                        })
                    )
                });
            syntax = DefineCommand(
                $"dns {orderLoc} {domain}" +
                $" --tenant-id tenantId --client-id clientId --client-secret abcd1234" +
                $" --subscription-id {Guid.NewGuid()} --resource-group {resourceGroup}");
            await Assert.ThrowsAsync<CertesCliException>(() => cmd.Execute(syntax));
        }

        [Fact]
        public async Task ExecuteWithoutAzureInfo()
        {
            var domain = "www.certes.com";
            var orderLoc = new Uri("http://acme.com/o/1");
            var resourceGroup = "resGroup";

            var challengeLoc = new Uri("http://acme.com/o/1/c/2");
            var authzLoc = new Uri("http://acme.com/o/1/a/1");
            var authz = new Authorization
            {
                Identifier = new Identifier
                {
                    Type = IdentifierType.Dns,
                    Value = domain
                },
                Challenges = new[]
                {
                    new Challenge
                    {
                        Token = "dns-token",
                        Type = ChallengeTypes.Dns01,
                    },
                    new Challenge
                    {
                        Token = "http-token",
                        Type = ChallengeTypes.Http01,
                    },
                }
            };

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(LetsEncryptV2);
            settingsMock.Setup(m => m.GetAccountKey(LetsEncryptV2)).ReturnsAsync(GetKeyV2());
            settingsMock.Setup(m => m.GetAzureSettings()).ReturnsAsync(new AzureSettings());
            
            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            var dnsMock = new Mock<IDnsManagementClient>(MockBehavior.Strict);

            var cmd = new AzureDnsCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object, _ => dnsMock.Object);

            var syntax = DefineCommand(
                $"dns {orderLoc} {domain}" +
                $" --resource-group {resourceGroup}");
            await Assert.ThrowsAsync<CertesCliException>(() => cmd.Execute(syntax));
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"dns http://acme.com/o/1 www.abc.com --server {LetsEncryptStagingV2}"
                + " --tenant-id tenantId --client-id clientId --client-secret abcd1234"
                + " --subscription-id subscriptionId --resource-group resGroup";
            var syntax = DefineCommand(args);

            Assert.Equal("dns", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);
            ValidateParameter(syntax, "order-id", new Uri("http://acme.com/o/1"));
            ValidateParameter(syntax, "domain", "www.abc.com");
            ValidateOption(syntax, "tenant-id", "tenantId");
            ValidateOption(syntax, "client-id", "clientId");
            ValidateOption(syntax, "client-secret", "abcd1234");
            ValidateOption(syntax, "subscription-id", "subscriptionId");
            ValidateOption(syntax, "resource-group", "resGroup");

            syntax = DefineCommand("noop");
            Assert.NotEqual("dns", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new AzureDnsCommand(
                NoopSettings(), (u, k) => new Mock<IAcmeContext>().Object, new FileUtil(), null);
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
