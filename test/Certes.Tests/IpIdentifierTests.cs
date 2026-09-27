using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Json;
using Certes.Jws;
using Moq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using Xunit;
using Directory = Certes.Acme.Resource.Directory;

namespace Certes
{
    public class IpIdentifierTests
    {
        [Fact]
        public void IpIdentifierTypeUsesRfc8738WireValue()
        {
            var json = JsonSerializer.Serialize(new Identifier { Type = IdentifierType.Ip, Value = "192.0.2.1" }, JsonUtil.CreateSettings());
            Assert.Equal("{\"type\":\"ip\",\"value\":\"192.0.2.1\"}", json);
            Assert.Equal(IdentifierType.Ip, JsonSerializer.Deserialize<Identifier>("{\"type\":\"ip\",\"value\":\"::1\"}", JsonUtil.CreateSettings()).Type);
            Assert.Equal(IdentifierType.Dns, JsonSerializer.Deserialize<Identifier>("{\"type\":\"dns\",\"value\":\"a.example\"}", JsonUtil.CreateSettings()).Type);
        }

        [Theory]
        [InlineData("192.0.2.1", "192.0.2.1")]
        [InlineData("0.0.0.0", "0.0.0.0")]
        [InlineData("255.255.255.255", "255.255.255.255")]
        [InlineData("2001:DB8:0:0:0:0:0:1", "2001:db8::1")]
        [InlineData("2001:db8::1", "2001:db8::1")]
        [InlineData("::ffff:192.0.2.1", "::ffff:192.0.2.1")]
        public void ValidAddressesAreNormalized(string input, string expected)
        {
            Assert.True(IpAddressUtil.TryNormalize(input, out var normalized));
            Assert.Equal(expected, normalized);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("192.0.2")]
        [InlineData("192.0.2.1.5")]
        [InlineData("192.000.2.1")]
        [InlineData("01.2.3.4")]
        [InlineData("256.1.1.1")]
        [InlineData("+1.2.3.4")]
        [InlineData(" 192.0.2.1")]
        [InlineData("0x7f.0.0.1")]
        [InlineData("fe80::1%eth0")]
        [InlineData("example.com")]
        [InlineData("2001:db8::g")]
        public void InvalidAddressesAreRejected(string input)
            => Assert.False(IpAddressUtil.TryNormalize(input, out _));

        [Fact]
        public async Task TypedOrderSendsNormalizedIpAndDnsIdentifiers()
        {
            var (ctx, http, directory) = CreateContext(null);
            JwsPayload payload = null;
            http.Setup(m => m.Post<Order>(directory.NewOrder, It.IsAny<object>()))
                .Callback<Uri, object>((_, body) => payload = Assert.IsType<JwsPayload>(body))
                .ReturnsAsync(new AcmeHttpResponse<Order>(new Uri("http://acme.d/order/1"), new Order(), null, null));
            var identifiers = new List<Identifier>
            {
                new Identifier { Type = IdentifierType.Ip, Value = "2001:DB8:0::1" },
                new Identifier { Type = IdentifierType.Ip, Value = "192.0.2.1" },
                new Identifier { Type = IdentifierType.Dns, Value = "www.example.com" },
            };

            var order = await ctx.NewOrder(identifiers);

            Assert.Equal(new Uri("http://acme.d/order/1"), order.Location);
            using var wire = JsonDocument.Parse(JwsConvert.FromBase64String(payload.Payload));
            var sent = wire.RootElement.GetProperty("identifiers").EnumerateArray()
                .Select(e => (e.GetProperty("type").GetString(), e.GetProperty("value").GetString())).ToArray();
            Assert.Equal(new[] { ("ip", "2001:db8::1"), ("ip", "192.0.2.1"), ("dns", "www.example.com") }, sent);
            Assert.Equal("2001:DB8:0::1", identifiers[0].Value);
        }

        [Fact]
        public async Task TypedProfileAndReplacementOrdersSendIpIdentifiers()
        {
            var (ctx, http, directory) = CreateContext(new Dictionary<string, string> { ["shortlived"] = "Six days" });
            var payloads = new List<JwsPayload>();
            http.Setup(m => m.Post<Order>(directory.NewOrder, It.IsAny<object>()))
                .Callback<Uri, object>((_, body) => payloads.Add(Assert.IsType<JwsPayload>(body)))
                .ReturnsAsync(new AcmeHttpResponse<Order>(new Uri("http://acme.d/order/1"), new Order(), null, null));
            var ip = new[] { new Identifier { Type = IdentifierType.Ip, Value = "192.0.2.1" } };

            await ctx.NewOrderWithProfile(ip, "shortlived", replacedCertificateId: "abc.AQ");
            await ctx.NewReplacementOrder(ip, "abc.AQ");

            Assert.Equal(2, payloads.Count);
            using var profiled = JsonDocument.Parse(JwsConvert.FromBase64String(payloads[0].Payload));
            Assert.Equal("shortlived", profiled.RootElement.GetProperty("profile").GetString());
            Assert.Equal("abc.AQ", profiled.RootElement.GetProperty("replaces").GetString());
            Assert.Equal("ip", profiled.RootElement.GetProperty("identifiers")[0].GetProperty("type").GetString());
            using var replacement = JsonDocument.Parse(JwsConvert.FromBase64String(payloads[1].Payload));
            Assert.Equal("ip", replacement.RootElement.GetProperty("identifiers")[0].GetProperty("type").GetString());
            Assert.Equal("abc.AQ", replacement.RootElement.GetProperty("replaces").GetString());
        }

        [Theory]
        [InlineData(IdentifierType.Ip, "1")]
        [InlineData(IdentifierType.Ip, "example.com")]
        [InlineData(IdentifierType.Ip, "fe80::1%eth0")]
        [InlineData(IdentifierType.Dns, "")]
        [InlineData(IdentifierType.Ip, null)]
        public async Task InvalidTypedIdentifiersAreRejectedBeforeAnyRequest(IdentifierType type, string value)
        {
            var ctx = new Mock<IAcmeContext>(MockBehavior.Strict);
            var ids = new[] { new Identifier { Type = type, Value = value } };

            Assert.Equal("identifiers", (await Assert.ThrowsAsync<ArgumentException>(() => ctx.Object.NewOrder(ids))).ParamName);
            await Assert.ThrowsAsync<ArgumentException>(() => ctx.Object.NewReplacementOrder(ids, "abc.AQ"));
            await Assert.ThrowsAsync<ArgumentException>(() => ctx.Object.NewOrderWithProfile(ids, "shortlived"));
            await Assert.ThrowsAsync<ArgumentNullException>(() => ctx.Object.NewOrder((IList<Identifier>)null));
            await Assert.ThrowsAsync<ArgumentException>(() => ctx.Object.NewOrder(new Identifier[] { null }));
            ctx.VerifyNoOtherCalls();
        }

        [Fact]
        public void CsrEncodesIpAddressesAsIpSans()
        {
            var builder = new Certes.Pkcs.CertificationRequestBuilder(KeyFactory.NewKey(KeyAlgorithm.ES256));
            builder.SubjectAlternativeNames.Add("192.0.2.1");
            builder.SubjectAlternativeNames.Add("2001:db8::1");
            builder.SubjectAlternativeNames.Add("www.example.com");

            var names = ReadSans(builder.Generate());

            Assert.Equal(GeneralName.IPAddress, names[0].TagNo);
            Assert.Equal(IPAddress.Parse("192.0.2.1").GetAddressBytes(), Asn1OctetString.GetInstance(names[0].Name).GetOctets());
            Assert.Equal(GeneralName.IPAddress, names[1].TagNo);
            Assert.Equal(IPAddress.Parse("2001:db8::1").GetAddressBytes(), Asn1OctetString.GetInstance(names[1].Name).GetOctets());
            Assert.Equal(GeneralName.DnsName, names[2].TagNo);
            Assert.Equal("www.example.com", DerIA5String.GetInstance(names[2].Name).GetString());
        }

        [Fact]
        public async Task FinalizeOmitsCommonNameForIpOnlyOrders()
        {
            var csr = await FinalizeWith("192.0.2.1");
            Assert.Empty(csr.GetCertificationRequestInfo().Subject.GetValueList(X509Name.CN));
            Assert.Equal(GeneralName.IPAddress, ReadSans(csr.GetEncoded()).Single().TagNo);
        }

        [Fact]
        public async Task FinalizeUsesFirstDnsNameWithinCommonNameLimit()
        {
            var longName = new string('a', 60) + ".example";
            var csr = await FinalizeWith("192.0.2.1", longName, "short.example");
            Assert.Equal(new[] { "short.example" }, csr.GetCertificationRequestInfo().Subject.GetValueList(X509Name.CN));
            Assert.Equal(3, ReadSans(csr.GetEncoded()).Length);
        }

        [Fact]
        public async Task FinalizeKeepsExistingCommonNameBehaviorForDns()
        {
            var csr = await FinalizeWith("www.example.com", "mail.example.com");
            Assert.Equal(new[] { "www.example.com" }, csr.GetCertificationRequestInfo().Subject.GetValueList(X509Name.CN));
        }

        [Theory]
        [InlineData("192.0.2.1")]
        [InlineData("2001:db8::1")]
        public void TlsAlpnCertificateUsesIpSanForIpIdentifiers(string address)
        {
            var pem = KeyFactory.NewKey(KeyAlgorithm.ES256).TlsAlpnCertificate("token", address, KeyFactory.NewKey(KeyAlgorithm.ES256));
            var certificate = new X509CertificateParser().ReadCertificate(Encoding.UTF8.GetBytes(pem));
            var name = Assert.Single(GeneralNames.GetInstance(X509ExtensionUtilities.FromExtensionValue(
                certificate.GetExtensionValue(X509Extensions.SubjectAlternativeName))).GetNames());
            Assert.Equal(GeneralName.IPAddress, name.TagNo);
            Assert.Equal(IPAddress.Parse(address).GetAddressBytes(), Asn1OctetString.GetInstance(name.Name).GetOctets());
        }

        private static async Task<Pkcs10CertificationRequest> FinalizeWith(params string[] values)
        {
            var order = new Mock<IOrderContext>(MockBehavior.Strict);
            order.Setup(m => m.Resource()).ReturnsAsync(new Order
            {
                Identifiers = values.Select(v => new Identifier
                {
                    Type = IpAddressUtil.TryParse(v, out _) ? IdentifierType.Ip : IdentifierType.Dns,
                    Value = v,
                }).ToArray(),
            });
            byte[] der = null;
            order.Setup(m => m.Finalize(It.IsAny<byte[]>())).Callback<byte[]>(d => der = d).ReturnsAsync(new Order());

            await order.Object.Finalize(new CsrInfo(), KeyFactory.NewKey(KeyAlgorithm.ES256));

            return new Pkcs10CertificationRequest(der);
        }

        private static GeneralName[] ReadSans(byte[] der)
        {
            var extensions = new Pkcs10CertificationRequest(der).GetRequestedExtensions();
            return GeneralNames.GetInstance(extensions.GetExtension(X509Extensions.SubjectAlternativeName).GetParsedValue()).GetNames();
        }

        private static (AcmeContext, Mock<IAcmeHttpClient>, Directory) CreateContext(IDictionary<string, string> profiles)
        {
            var http = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            var directory = new Directory(null, Helper.MockDirectoryV2.NewAccount, Helper.MockDirectoryV2.NewOrder, null, null,
                new DirectoryMeta(null, null, null, null, profiles), null);
            var uri = new Uri("http://acme.d/directory");
            http.Setup(m => m.Get<Directory>(uri)).ReturnsAsync(new AcmeHttpResponse<Directory>(null, directory, null, null));
            http.Setup(m => m.ConsumeNonce()).ReturnsAsync("nonce");
            http.Setup(m => m.Post<Account>(directory.NewAccount, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<Account>(new Uri("http://acme.d/account/1"), new Account(), null, null));
            return (new AcmeContext(uri, Helper.GetKeyV2(), http.Object), http, directory);
        }
    }
}
