using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;
using Moq;
using Xunit;

namespace Certes.Acme
{
    public class DnsPersistChallengeTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PostsEmptyObjectOnlyAfterCheckingChallenge(bool malformed)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var directoryUrl = new Uri("https://ca.example/directory");
            var newAccount = new Uri("https://ca.example/new-account");
            var challengeUrl = new Uri("https://ca.example/chall/1");
            var payloads = new List<string>();
            var resource = new Challenge { Type = ChallengeTypes.DnsPersist01,
                IssuerDomainNames = malformed ? null : new[] { "ca.example" } };
            var http = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            http.Setup(h => h.Get<Directory>(directoryUrl, token)).ReturnsAsync(new AcmeHttpResponse<Directory>(null,
                new Directory(null, newAccount, null, null, null,
                    new DirectoryMeta(null, null, null, null, null, null, "https://ca.example/hash/"), null), null, null));
            http.Setup(h => h.ConsumeNonce(token)).ReturnsAsync("nonce");
            http.Setup(h => h.Post<Account>(newAccount, It.IsAny<object>(), token)).ReturnsAsync(
                new AcmeHttpResponse<Account>(new Uri("https://ca.example/acct/1"), new Account(), null, null));
            http.Setup(h => h.Post<Challenge>(challengeUrl, It.IsAny<object>(), token))
                .Callback<Uri, object, CancellationToken>((_, body, _) => payloads.Add(
                    Encoding.UTF8.GetString(JwsConvert.FromBase64String(((JwsPayload)body).Payload))))
                .ReturnsAsync(new AcmeHttpResponse<Challenge>(challengeUrl, resource, null, null));
            var context = new AcmeContext(directoryUrl, KeyFactory.NewKey(KeyAlgorithm.ES256), http.Object);
            var challenge = new ChallengeContext(context, challengeUrl, ChallengeTypes.DnsPersist01, null);
            Assert.Throws<InvalidOperationException>(() => challenge.KeyAuthz);
            if (malformed)
            {
                await Assert.ThrowsAsync<AcmeException>(() => challenge.Validate(token));
                Assert.Equal(new[] { "" }, payloads);
            }
            else
            {
                Assert.Same(resource, await challenge.Validate(token));
                Assert.Equal(new[] { "", "{}" }, payloads);
            }
        }
    }
}
