using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Rest.Azure;
using Moq;
using Newtonsoft.Json;
using Xunit;
using static Certes.Acme.WellKnownServers;
using static Certes.Cli.CliTestHelper;
using static Certes.Helper;

namespace Certes.Cli.Commands
{
    public class AzureAppCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var domain = "www.certes.com";
            var orderLoc = new Uri("http://acme.com/o/1");
            var resourceGroup = "resGroup";
            var appName = "my-app";
            var appSlot = "staging";
            var keyPath = "./cert-key.pem";

            var certChainContent = string.Join(
                Environment.NewLine,
                File.ReadAllText("./Data/leaf-cert.pem"),
                File.ReadAllText("./Data/test-ca2.pem"),
                File.ReadAllText("./Data/test-root.pem"));
            var certChain = new CertificateChain(certChainContent);

            var order = new Order
            {
                Certificate = new Uri("http://acme.com/o/1/cert")
            };

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(LetsEncryptV2);
            settingsMock.Setup(m => m.GetAccountKey(LetsEncryptV2)).ReturnsAsync(GetKeyV2());
            settingsMock.Setup(m => m.GetAzureSettings()).ReturnsAsync(new AzureSettings
            {
                ClientId = "clientId",
                ClientSecret = "secret",
                SubscriptionId = Guid.NewGuid().ToString("N"),
                TalentId = Guid.NewGuid().ToString("N"),
            });

            var orderMock = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock.Setup(m => m.Location).Returns(orderLoc);
            orderMock.Setup(m => m.Resource()).ReturnsAsync(order);
            orderMock.Setup(m => m.Download()).ReturnsAsync(certChain);

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);
            ctxMock.Setup(m => m.Order(orderLoc)).Returns(orderMock.Object);
            ctxMock.SetupGet(m => m.AccountKey).Returns(GetKeyV2());

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.ReadAllText(keyPath))
                .ReturnsAsync(KeyFactory.NewKey(KeyAlgorithm.ES256).ToPem());

            var appSvcMock = new Mock<IWebSiteManagementClient>(MockBehavior.Strict);
            var certOpMock = new Mock<ICertificatesOperations>(MockBehavior.Strict);
            var webAppOpMock = new Mock<IWebAppsOperations>(MockBehavior.Strict);

            appSvcMock.SetupSet(m => m.SubscriptionId);
            appSvcMock.SetupGet(m => m.WebApps).Returns(webAppOpMock.Object);
            appSvcMock.SetupGet(m => m.Certificates).Returns(certOpMock.Object);
            appSvcMock.Setup(m => m.Dispose());

            certOpMock.Setup(m => m.ListByResourceGroupWithHttpMessagesAsync(resourceGroup, default, default))
                .ReturnsAsync(new AzureOperationResponse<IPage<CertificateInner>>
                {
                    Body = JsonConvert.DeserializeObject<Page<CertificateInner>>(
                        JsonConvert.SerializeObject(new
                        {
                            value = new CertificateInner[0]
                        })
                    )
                });
            certOpMock.Setup(m => m.CreateOrUpdateWithHttpMessagesAsync(resourceGroup, It.IsAny<string>(), It.IsAny<CertificateInner>(), default, default))
                .ReturnsAsync((string r, string n, CertificateInner c, Dictionary<string, List<string>> h, CancellationToken t)
                    => new AzureOperationResponse<CertificateInner> { Body = c });

            webAppOpMock.Setup(m => m.CreateOrUpdateHostNameBindingWithHttpMessagesAsync(
                resourceGroup, appName, domain, It.IsAny<HostNameBindingInner>(), default, default))
                .ReturnsAsync((string r, string a, string n, HostNameBindingInner d, Dictionary<string, List<string>> h, CancellationToken t)
                    => new AzureOperationResponse<HostNameBindingInner> { Body = d });
            webAppOpMock.Setup(m => m.GetWithHttpMessagesAsync(resourceGroup, appName, default, default))
                .ReturnsAsync(new AzureOperationResponse<SiteInner>
                {
                    Body = new SiteInner
                    {
                        Location = "Canada"
                    }
                });
            webAppOpMock.Setup(m => m.GetSlotWithHttpMessagesAsync(resourceGroup, appName, appSlot, default, default))
                .ReturnsAsync(new AzureOperationResponse<SiteInner>
                {
                    Body = new SiteInner
                    {
                        Location = "Canada"
                    }
                });

            var envMock = new Mock<IEnvironmentVariables>(MockBehavior.Strict);

            var cmd = new AzureAppCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object, envMock.Object, _ => appSvcMock.Object);

            var args = $"app {orderLoc} {domain} {appName} --private-key {keyPath}"
                + $" --talent-id talentId --client-id clientId --client-secret abcd1234"
                + $" --subscription-id {Guid.NewGuid()} --resource-group {resourceGroup}";
            var syntax = DefineCommand(args);
            dynamic ret = await cmd.Execute(syntax);
            Assert.NotNull(ret.data);

            webAppOpMock.Verify(m => m.CreateOrUpdateHostNameBindingWithHttpMessagesAsync(
                resourceGroup, appName, domain, It.IsAny<HostNameBindingInner>(), default, default), Times.Once);

            // with deployment slot
            webAppOpMock.Setup(m => m.CreateOrUpdateHostNameBindingSlotWithHttpMessagesAsync(
                resourceGroup, appName, domain, It.IsAny<HostNameBindingInner>(), appSlot, default, default))
                .ReturnsAsync((string r, string a, string n, HostNameBindingInner d, string s, Dictionary<string, List<string>> h, CancellationToken t)
                    => new AzureOperationResponse<HostNameBindingInner> { Body = d });

            args = $"app {orderLoc} {domain} {appName} --private-key {keyPath}"
                + $" --slot {appSlot}"
                + $" --talent-id talentId --client-id clientId --client-secret abcd1234"
                + $" --subscription-id {Guid.NewGuid()} --resource-group {resourceGroup}";
            syntax = DefineCommand(args);
            ret = await cmd.Execute(syntax);
            Assert.NotNull(ret.data);
            webAppOpMock.Verify(m => m.CreateOrUpdateHostNameBindingSlotWithHttpMessagesAsync(
                resourceGroup, appName, domain, It.IsAny<HostNameBindingInner>(), appSlot, default, default), Times.Once);

            var cert = new X509Certificate2(certChain.Certificate.ToDer());
            certOpMock.Setup(m => m.ListByResourceGroupWithHttpMessagesAsync(resourceGroup, default, default))
                .ReturnsAsync(new AzureOperationResponse<IPage<CertificateInner>>
                {
                    Body = JsonConvert.DeserializeObject<Page<CertificateInner>>(
                        JsonConvert.SerializeObject(new
                        {
                            value = new CertificateInner[]
                            {
                                new CertificateInner("certes", thumbprint: cert.Thumbprint)
                            }
                        })
                    )
                });

            args = $"app {orderLoc} {domain} {appName} --private-key {keyPath}"
                + $" --talent-id talentId --client-id clientId --client-secret abcd1234"
                + $" --subscription-id {Guid.NewGuid()} --resource-group {resourceGroup}";
            syntax = DefineCommand(args);
            ret = await cmd.Execute(syntax);
            Assert.NotNull(ret.data);
            Assert.Equal(cert.Thumbprint, ret.data.Thumbprint);

            // order incompleted
            orderMock.Setup(m => m.Resource()).ReturnsAsync(new Order());
            args = $"app {orderLoc} {domain} {appName} --private-key {keyPath}"
                + $" --talent-id talentId --client-id clientId --client-secret abcd1234"
                + $" --subscription-id {Guid.NewGuid()} --resource-group {resourceGroup}";
            syntax = DefineCommand(args);
            await Assert.ThrowsAsync<Exception>(() => cmd.Execute(syntax));
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"app http://acme.com/o/1 www.abc.com my-app --private-key ./cert-key.pem"
                + " --slot staging"
                + " --talent-id talentId --client-id clientId --client-secret abcd1234"
                + " --subscription-id subscriptionId --resource-group resGroup";
            var syntax = DefineCommand(args);

            Assert.Equal("app", syntax.ActiveCommand.Value);
            ValidateParameter(syntax, "order-id", new Uri("http://acme.com/o/1"));
            ValidateParameter(syntax, "app", "my-app");
            ValidateParameter(syntax, "domain", "www.abc.com");
            ValidateParameter(syntax, "private-key", "./cert-key.pem");
            ValidateOption(syntax, "talent-id", "talentId");
            ValidateOption(syntax, "client-id", "clientId");
            ValidateOption(syntax, "client-secret", "abcd1234");
            ValidateOption(syntax, "subscription-id", "subscriptionId");
            ValidateOption(syntax, "resource-group", "resGroup");
            ValidateOption(syntax, "slot", "staging");

            syntax = DefineCommand("noop");
            Assert.NotEqual("app", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new AzureAppCommand(
                NoopSettings(), (u, k) => new Mock<IAcmeContext>().Object, new FileUtil(), null, null);
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
