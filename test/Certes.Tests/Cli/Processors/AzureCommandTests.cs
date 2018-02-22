using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Options;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Rest.Azure;
using Moq;
using Newtonsoft.Json;
using Xunit;

using ZoneInnerPage = Microsoft.Azure.Management.Dns.Fluent.Models.Page<Microsoft.Azure.Management.Dns.Fluent.Models.ZoneInner>;

namespace Certes.Cli.Processors
{
    [Collection(nameof(ContextFactory))]
    public class AzureCommandTests
    {
        [Fact]
        public void CanParseCommnad()
        {
            var talentId = Guid.NewGuid();
            var subscriptionId = Guid.NewGuid();
            var resourceGroup = "res";
            var orderUir = new Uri("http://acme.d/order/1");
            var host = "example-domain.com";

            var options = Parse("noop");
            Assert.Null(options);

            // deploy dns using service-principal
            options = Parse(
                $"azure dns --user certes --pwd abcd1234 " +
                $"--resourceGroup {resourceGroup} --talent {talentId} --subscription {subscriptionId} " +
                $"--order {orderUir} {host}");
            Assert.Equal(AzureAction.Dns, options.Action);
            Assert.Equal("certes", options.UserName);
            Assert.Equal("abcd1234", options.Password);
            Assert.Equal(talentId, options.Talent);
            Assert.Equal(subscriptionId, options.Subscription);
            Assert.Equal(orderUir, options.OrderUri);
            Assert.Equal(host, options.Value);
            Assert.Equal(resourceGroup, options.ResourceGroup);
            Assert.Equal(AzureCloudEnvironment.Default, options.CloudEnvironment);
        }

        [Fact]
        public async Task CanSetSsl()
        {
            var keyPath = $"./Data/{nameof(CanSetDns)}/key.pem";
            var certKeyPath = $"./Data/{nameof(CanSetDns)}/cert.pem";
            Helper.SaveKey(keyPath);

            if (File.Exists(certKeyPath))
            {
                File.Delete(certKeyPath);
            }

            var hosts = new List<string> { "www.example.com" };
            var order = new
            {
                uri = new Uri("http://acme.d/order/1"),
                data = new Order
                {
                    Status = OrderStatus.Valid,
                    Identifiers = hosts.Select(h => new Identifier { Value = h, Type = IdentifierType.Dns }).ToArray(),
                },
            };

            var orderMock = new Mock<IOrderContext>();
            var ctxMock = new Mock<IAcmeContext>();

            var appSvcMock = new Mock<IWebSiteManagementClient>();
            var certOpMock = new Mock<ICertificatesOperations>();
            var webAppOpMock = new Mock<IWebAppsOperations>();

            ContextFactory.Create = (uri, key) => ctxMock.Object;
            ContextFactory.CreateAppServiceManagementClient = (c) => appSvcMock.Object;

            ctxMock.SetupGet(c => c.AccountKey).Returns(Helper.GetKeyV2());
            ctxMock.Setup(c => c.Order(order.uri)).Returns(orderMock.Object);

            orderMock.Setup(c => c.Location).Returns(order.uri);
            orderMock.Setup(m => m.Resource()).ReturnsAsync(order.data);
            orderMock.Setup(m => m.Download())
                .ReturnsAsync(new CertificateChain(
                    File.ReadAllText("./Data/cert-es256.pem") + 
                    Environment.NewLine +
                    File.ReadAllText("./Data/test-ca2.pem")));

            appSvcMock.SetupGet(m => m.WebApps).Returns(webAppOpMock.Object);
            appSvcMock.SetupGet(m => m.Certificates).Returns(certOpMock.Object);

            certOpMock.Setup(m => m.CreateOrUpdateWithHttpMessagesAsync("res", hosts[0], It.IsAny<CertificateInner>(), default, default))
                .ReturnsAsync((string r, string n, CertificateInner c, Dictionary<string, List<string>> h, CancellationToken t)
                    => new AzureOperationResponse<CertificateInner> { Body = c });

            webAppOpMock.Setup(m => m.CreateOrUpdateHostNameBindingWithHttpMessagesAsync(
                "res", "certes", hosts[0], It.IsAny<HostNameBindingInner>(), default, default))
                .ReturnsAsync((string r, string a, string n, HostNameBindingInner d, Dictionary<string, List<string>> h, CancellationToken t)
                    => new AzureOperationResponse<HostNameBindingInner> { Body = d });

            var userSettings = new UserSettings();
            var proc = new AzureCommand(new AzureOptions
            {
                Action = AzureAction.Ssl,
                Value = hosts[0],
                OrderUri = order.uri,
                PrivateKey = certKeyPath,
                Issuers = new List<string> { "./Data/test-root.pem" }.AsReadOnly(),
                
                UserName = "certes",
                Password = "abcd1234",
                Talent = Guid.NewGuid(),
                Subscription = Guid.NewGuid(),
                ResourceGroup = "res",
                AppServiceName = "certes",
                Path = keyPath,
            }, userSettings);

            var ret = await proc.Process();
            Assert.True(File.Exists(certKeyPath));
        }

        [Fact]
        public async Task CanSetDns()
        {
            var keyPath = $"./Data/{nameof(CanSetDns)}/key.pem";
            Helper.SaveKey(keyPath);

            var hosts = new List<string> { "www.certes.com", "mail.certes.com" };
            var order = new
            {
                uri = new Uri("http://acme.d/order/1"),
                data = new Order
                {
                    Identifiers = hosts.Select(h => new Identifier { Value = h, Type = IdentifierType.Dns }).ToArray(),
                    Authorizations = hosts.Select((i, a) => new Uri("http://acme.d/authz/{i}")).ToArray(),
                },
            };

            var authz = new
            {
                uri = order.data.Authorizations[0],
                data = new Acme.Resource.Authorization
                {
                    Identifier = new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                }
            };

            var expectedRecordSet = new { data = new RecordSetInner(id: Guid.NewGuid().ToString()) };

            var challengeMock = new Mock<IChallengeContext>();
            var authzMock = new Mock<IAuthorizationContext>();
            var orderMock = new Mock<IOrderContext>();
            var ctxMock = new Mock<IAcmeContext>();

            var dnsMock = new Mock<IDnsManagementClient>();
            var zonesOpMock = new Mock<IZonesOperations>();
            var recordSetsOpMock = new Mock<IRecordSetsOperations>();

            ContextFactory.Create = (uri, key) => ctxMock.Object;
            ContextFactory.CreateDnsManagementClient = (c) => dnsMock.Object;

            ctxMock.SetupGet(c => c.AccountKey).Returns(Helper.GetKeyV2());
            ctxMock.Setup(c => c.Order(order.uri)).Returns(orderMock.Object);

            orderMock.Setup(c => c.Authorizations()).ReturnsAsync(order.data.Identifiers.Select(a => authzMock.Object));
            orderMock.Setup(c => c.Location).Returns(order.uri);

            authzMock.Setup(c => c.Location).Returns(order.data.Authorizations[0]);
            authzMock.Setup(c => c.Resource()).ReturnsAsync(authz.data);
            authzMock.Setup(c => c.Challenges()).ReturnsAsync(new[] { challengeMock.Object });

            challengeMock.SetupGet(m => m.Token).Returns("abcd");
            challengeMock.SetupGet(m => m.Type).Returns(Acme.Resource.ChallengeTypes.Dns01);

            dnsMock.SetupGet(m => m.Zones).Returns(zonesOpMock.Object);
            dnsMock.SetupGet(m => m.RecordSets).Returns(recordSetsOpMock.Object);

            zonesOpMock.Setup(m => m.ListWithHttpMessagesAsync(default, default, default))
                .ReturnsAsync(new AzureOperationResponse<IPage<ZoneInner>>
                {
                    Body = JsonConvert.DeserializeObject<ZoneInnerPage>(
                        JsonConvert.SerializeObject(new
                        {
                            value = new[] { new ZoneInner(id: "/s/abcd1234/resourceGroups/res/a", name: "certes.com") }
                        })
                    )
                });

            recordSetsOpMock.Setup(m => m.CreateOrUpdateWithHttpMessagesAsync("res", "certes.com", "_acme-challenge.www", RecordType.TXT, It.IsAny<RecordSetInner>(), default, default, default, default))
                .ReturnsAsync(new AzureOperationResponse<RecordSetInner>
                {
                    Body = expectedRecordSet.data
                });

            var userSettings = new UserSettings();
            var proc = new AzureCommand(new AzureOptions
            {
                Action = AzureAction.Dns,
                Value = hosts[0],
                OrderUri = order.uri,
                UserName = "certes",
                Password = "abcd1234",
                Talent = Guid.NewGuid(),
                Subscription = Guid.NewGuid(),
                ResourceGroup = "res",
                Path = keyPath,
            }, userSettings);

            var ret = await proc.Process();
            Assert.Equal(JsonConvert.SerializeObject(expectedRecordSet), JsonConvert.SerializeObject(ret));
        }

        private AzureOptions Parse(string cmd)
        {
            AzureOptions options = null;
            ArgumentSyntax.Parse(cmd.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                options = AzureCommand.TryParse(syntax);
            });

            return options;
        }
    }
}
