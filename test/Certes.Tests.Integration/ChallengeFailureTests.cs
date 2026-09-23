using System;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Xunit;

namespace Certes
{
    public class ChallengeFailureTests
    {
        [Theory]
        [InlineData(ChallengeTypes.Http01)]
        [InlineData(ChallengeTypes.Dns01)]
        [InlineData("tls-alpn-01")] // TlsAlpn01 is a public property, not an attribute-compatible constant.
        public async Task MissingResponseFailsValidation(string type)
        {
            var directory = await IntegrationHelper.GetAcmeUriV2();
            var context = IntegrationHelper.NewAcmeContext(directory);
            await context.NewAccount(new[] { "mailto:negative@example.test" }, true);
            var order = await context.NewOrder(new[] { $"missing-{Guid.NewGuid():N}.example.test" });
            var authorization = (await order.Authorizations()).Single();
            var challenge = await authorization.Challenge(type);
            await challenge.Validate();
            await IntegrationHelper.WaitForAuthorization(authorization, AuthorizationStatus.Invalid);
            var result = await challenge.Resource();
            Assert.Equal(ChallengeStatus.Invalid, result.Status);
            Assert.NotNull(result.Error);
        }
    }
}
