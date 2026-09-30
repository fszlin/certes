using System.Threading.Tasks;
using Certes.Acme;
using Xunit;
using Xunit.Abstractions;

using static Certes.Helper;
using static Certes.IntegrationHelper;

namespace Certes
{
    public partial class AcmeContextIntegration
    {
        public class DnsPersistTests : AcmeContextIntegration
        {
            public DnsPersistTests(ITestOutputHelper output) : base(output) { }

            [Fact]
            public async Task RejectsPebbleLegacyDraftBeforeAcknowledgingChallenge()
            {
                // Pebble 2.10.1 implements the cleartext-account, hyphenated-field draft.
                // This is a compatibility rejection test, not evidence of -02 issuance.
                var context = NewAcmeContext(await GetAcmeUriV2(), GetKeyV2());
                var order = await context.NewOrder(new[] { "dns-persist.example.test" });
                var authorization = Assert.Single(await order.Authorizations());
                var challenge = await authorization.DnsPersist();
                Assert.NotNull(challenge);
                var resource = await challenge.Resource();
                Assert.Null(resource.Token);
                Assert.Null(resource.IssuerDomainNames);
                await Assert.ThrowsAsync<AcmeException>(() => context.GetDnsPersistRecord(
                    new Acme.Resource.Authorization
                    {
                        Identifier = new Acme.Resource.Identifier { Type = Acme.Resource.IdentifierType.Dns, Value = "dns-persist.example.test" },
                    }, resource));
                await Assert.ThrowsAsync<AcmeException>(() => challenge.Validate());
                Assert.Equal(Acme.Resource.ChallengeStatus.Pending, (await challenge.Resource()).Status);
                await ClearAuthorizations(order);
            }
        }
    }
}
