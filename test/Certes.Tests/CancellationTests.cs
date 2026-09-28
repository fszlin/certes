using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Jws;
using Moq;
using Xunit;

namespace Certes
{
    public class CancellationTests
    {
        private static readonly Uri DirectoryUri = new Uri("https://acme.test/directory");
        private static readonly Uri AccountUri = new Uri("https://acme.test/account/1");

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TransportPreservesReceivedResponseAfterLateCancellation(bool post)
        {
            using var cancellation = new CancellationTokenSource();
            using var http = new HttpClient(new Handler(async (_, _) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"status\":\"valid\"}", System.Text.Encoding.UTF8, "application/json"),
                };
                response.Headers.Location = AccountUri;
                await response.Content.LoadIntoBufferAsync();
                cancellation.Cancel();
                return response;
            }));
            var client = new AcmeHttpClient(DirectoryUri, http);
            var result = post
                ? await client.Post<Order>(DirectoryUri, new { }, cancellation.Token)
                : await client.Get<Order>(DirectoryUri, cancellation.Token);
            Assert.True(cancellation.IsCancellationRequested);
            Assert.Equal(AccountUri, result.Location);
            Assert.Equal(OrderStatus.Valid, result.Resource.Status);
        }

        [Theory]
        [InlineData("account")]
        [InlineData("order")]
        [InlineData("finalize")]
        [InlineData("key-change")]
        public async Task StateChangingOperationsPreserveSuccessfulResponse(string operation)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var endpoint = new Uri("https://acme.test/operation");
            var location = new Uri("https://acme.test/result/1");
            var transport = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            var directory = new Directory(endpoint, AccountUri, endpoint, endpoint, endpoint, null);
            transport.Setup(c => c.Get<Directory>(DirectoryUri, token)).ReturnsAsync(
                new AcmeHttpResponse<Directory>(DirectoryUri, directory, null, null));
            transport.Setup(c => c.ConsumeNonce(token)).ReturnsAsync("nonce");
            transport.Setup(c => c.Post<Account>(AccountUri, It.IsAny<object>(), token)).ReturnsAsync(
                new AcmeHttpResponse<Account>(AccountUri, new Account(), null, null));
            var context = new AcmeContext(DirectoryUri, Helper.GetKeyV2(), transport.Object);
            var returnedAccount = new Account();
            var returnedOrder = new Order { Status = OrderStatus.Valid };

            if (operation == "account")
            {
                transport.Setup(c => c.Post<Account>(AccountUri, It.IsAny<object>(), token))
                    .Callback(() => cancellation.Cancel()).ReturnsAsync(
                        new AcmeHttpResponse<Account>(location, returnedAccount, null, null));
                var result = await context.NewAccount("test@example.test", cancellationToken: token);
                Assert.Equal(location, result.Location);
                Assert.Same(result, await context.Account());
            }
            else if (operation == "key-change")
            {
                transport.Setup(c => c.Post<Account>(endpoint, It.IsAny<object>(), token))
                    .Callback(() => cancellation.Cancel()).ReturnsAsync(
                        new AcmeHttpResponse<Account>(AccountUri, returnedAccount, null, null));
                var newKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                Assert.Same(returnedAccount, await context.ChangeKey(newKey, token));
                Assert.Same(newKey, context.AccountKey);
            }
            else
            {
                transport.Setup(c => c.Post<Order>(endpoint, It.IsAny<object>(), token))
                    .Callback(() => cancellation.Cancel()).ReturnsAsync(
                        new AcmeHttpResponse<Order>(location, returnedOrder, null, null, 17));
                if (operation == "order")
                {
                    Assert.Equal(location, (await context.NewOrder(new[] { "example.test" }, cancellationToken: token)).Location);
                }
                else
                {
                    transport.Setup(c => c.Post<Order>(location, It.IsAny<object>(), token)).ReturnsAsync(
                        new AcmeHttpResponse<Order>(location, new Order { Finalize = endpoint }, null, null));
                    var order = context.Order(location);
                    Assert.Same(returnedOrder, await order.Finalize(new byte[] { 1 }, token));
                    Assert.Equal(17, order.RetryAfter);
                }
            }
            Assert.True(cancellation.IsCancellationRequested);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CancellationBetweenSigningAndSendPreventsPost(bool signer)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var transport = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            Task pending;
            if (signer)
            {
                transport.Setup(c => c.ConsumeNonce(token)).Callback(() => cancellation.Cancel()).ReturnsAsync("nonce");
                pending = transport.Object.Post<Account>(new JwsSigner(Helper.GetKeyV2()), AccountUri, new { }, true, 1, token);
            }
            else
            {
                var context = new Mock<IAcmeContext>(MockBehavior.Strict);
                context.Setup(c => c.Sign(It.IsAny<object>(), AccountUri, token))
                    .Callback(() => cancellation.Cancel()).ReturnsAsync(new JwsPayload());
                pending = transport.Object.Post<Account>(context.Object, AccountUri, new { }, true, token);
            }
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            transport.Verify(c => c.Post<Account>(It.IsAny<Uri>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReceivedTerminalErrorsAreNotReplacedByCancellation(bool signer)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var error = new AcmeError { Status = HttpStatusCode.BadRequest, Type = "urn:ietf:params:acme:error:malformed" };
            var transport = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            transport.Setup(c => c.Post<Account>(AccountUri, It.IsAny<object>(), token))
                .Callback(() => cancellation.Cancel()).ReturnsAsync(new AcmeHttpResponse<Account>(AccountUri, null, null, error));
            Task pending;
            if (signer)
            {
                transport.Setup(c => c.ConsumeNonce(token)).ReturnsAsync("nonce");
                pending = transport.Object.Post<Account>(new JwsSigner(Helper.GetKeyV2()), AccountUri, new { }, true, 1, token);
            }
            else
            {
                var context = new Mock<IAcmeContext>(MockBehavior.Strict);
                context.SetupGet(c => c.BadNonceRetryCount).Returns(1);
                context.Setup(c => c.Sign(It.IsAny<object>(), AccountUri, token)).ReturnsAsync(new JwsPayload());
                pending = transport.Object.Post<Account>(context.Object, AccountUri, new { }, true, token);
            }
            Assert.Same(error, (await Assert.ThrowsAsync<AcmeRequestException>(() => pending)).Error);
        }

        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        [InlineData("nonce-directory")]
        [InlineData("nonce-head")]
        public async Task CancelsInFlightTransport(string operation)
        {
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = new CancellationTokenSource();
            using var http = new HttpClient(new Handler(async (request, token) =>
            {
                if (operation == "nonce-head" && request.Method == HttpMethod.Get)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"newNonce\":\"https://acme.test/nonce\"}", System.Text.Encoding.UTF8, "application/json"),
                    };
                }

                Assert.True(token.CanBeCanceled);
                if (operation == "nonce-head")
                {
                    Assert.Equal(HttpMethod.Head, request.Method);
                }
                entered.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, token);
                throw new InvalidOperationException("Cancelled request must not complete.");
            }));
            var client = new AcmeHttpClient(DirectoryUri, http);
            Task pending = operation == "get" ? client.Get<string>(DirectoryUri, cancellation.Token) :
                operation == "post" ? client.Post<string>(DirectoryUri, new { }, cancellation.Token) :
                client.ConsumeNonce(cancellation.Token);

            await WithTimeout(entered.Task);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WithTimeout(pending));
            Assert.True(pending.IsCanceled);
        }

        [Fact]
        public async Task PreCancelledTokenDoesNotSendRequestsOrConsumeCachedNonce()
        {
            var sends = 0;
            using var http = new HttpClient(new Handler((_, _) =>
            {
                sends++;
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
                response.Headers.Add("Replay-Nonce", "cached-nonce");
                return Task.FromResult(response);
            }));
            var client = new AcmeHttpClient(DirectoryUri, http);
            await client.Get<string>(DirectoryUri);
            var token = new CancellationToken(true);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.Get<string>(DirectoryUri, token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.Post<string>(DirectoryUri, new { }, token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ConsumeNonce(token));
            Assert.Equal("cached-nonce", await client.ConsumeNonce());
            Assert.Equal(1, sends);
        }

        [Fact]
        public async Task CachedAccountAndDirectoryHonorCancellation()
        {
            var transport = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            transport.Setup(c => c.Get<Directory>(DirectoryUri, default)).ReturnsAsync(new AcmeHttpResponse<Directory>(
                DirectoryUri, new Directory(null, AccountUri, null, null, null, null), null, null));
            transport.Setup(c => c.ConsumeNonce(default)).ReturnsAsync("nonce");
            transport.Setup(c => c.Post<Account>(AccountUri, It.IsAny<object>(), default)).ReturnsAsync(
                new AcmeHttpResponse<Account>(AccountUri, new Account(), null, null));
            var context = new AcmeContext(DirectoryUri, Helper.GetKeyV2(), transport.Object);
            await context.GetDirectory();
            await context.Account();
            var count = transport.Invocations.Count;
            var token = new CancellationToken(true);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.GetDirectory(token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Account(token));
            Assert.Equal(count, transport.Invocations.Count);
            Assert.NotNull(await context.Account());
            Assert.NotNull(await context.GetDirectory());
            Assert.Equal(count, transport.Invocations.Count);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CancellationStopsBadNonceRetries(bool accountCreation)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var transport = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            var response = new AcmeHttpResponse<Account>(AccountUri, null, null, new AcmeError
            {
                Status = HttpStatusCode.BadRequest,
                Type = "urn:ietf:params:acme:error:badNonce",
            });
            transport.Setup(c => c.Post<Account>(AccountUri, It.IsAny<object>(), token))
                .Callback(() => cancellation.Cancel()).ReturnsAsync(response);

            Task pending;
            if (accountCreation)
            {
                transport.Setup(c => c.ConsumeNonce(token)).ReturnsAsync("nonce");
                pending = transport.Object.Post<Account>(new JwsSigner(Helper.GetKeyV2()), AccountUri, new { }, true, 8, token);
            }
            else
            {
                var context = new Mock<IAcmeContext>(MockBehavior.Strict);
                context.SetupGet(c => c.BadNonceRetryCount).Returns(8);
                context.Setup(c => c.Sign(It.IsAny<object>(), AccountUri, token)).ReturnsAsync(new JwsPayload());
                pending = transport.Object.Post<Account>(context.Object, AccountUri, new { }, true, token);
            }

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            transport.Verify(c => c.Post<Account>(AccountUri, It.IsAny<object>(), token), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CancelsPollingBeforeAndAfterFinalize(bool afterFinalize)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var order = new Mock<IOrderContext>(MockBehavior.Strict);
            var resource = new Order
            {
                Status = afterFinalize ? OrderStatus.Ready : OrderStatus.Pending,
                Identifiers = new[] { new Identifier { Type = IdentifierType.Dns, Value = "example.test" } },
            };
            order.Setup(c => c.Resource(token)).ReturnsAsync(resource);
            order.SetupGet(c => c.RetryAfter).Returns(900);
            if (afterFinalize)
            {
                order.Setup(c => c.Finalize(It.IsAny<byte[]>(), token))
                    .ReturnsAsync(new Order { Status = OrderStatus.Processing });
            }

            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = IOrderContextExtensions.Generate(order.Object, new CsrInfo(), Helper.GetKeyV2(), null, 60,
                (delay, actualToken) =>
                {
                    Assert.Equal(token, actualToken);
                    Assert.Equal(TimeSpan.FromMinutes(15), delay);
                    entered.TrySetResult(true);
                    return Task.Delay(delay, actualToken);
                }, token);
            await WithTimeout(entered.Task);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WithTimeout(pending));
            order.Verify(c => c.Resource(token), Times.Exactly(afterFinalize ? 2 : 1));
            order.Verify(c => c.Finalize(It.IsAny<byte[]>(), token), afterFinalize ? Times.Once() : Times.Never());
            order.Verify(c => c.Download(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CancellationStopsPagination()
        {
            using var cancellation = new CancellationTokenSource();
            var transport = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            transport.Setup(c => c.Get<OrderList>(AccountUri, cancellation.Token))
                .Callback(() => cancellation.Cancel()).ReturnsAsync(new AcmeHttpResponse<OrderList>(
                    AccountUri, new OrderList { Orders = new Uri[0] },
                    new[] { new { Name = "next", Uri = DirectoryUri } }.ToLookup(x => x.Name, x => x.Uri), null));
            var context = new Mock<IAcmeContext>();
            context.SetupGet(c => c.HttpClient).Returns(transport.Object);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new OrderListContext(context.Object, AccountUri).Orders(cancellation.Token));
            transport.Verify(c => c.Get<OrderList>(It.IsAny<Uri>(), cancellation.Token), Times.Once);
        }

        [Fact]
        public async Task AccountTaskExtensionsPreserveCancellationAndDoNotDeactivate()
        {
            using var cancellation = new CancellationTokenSource();
            var source = new TaskCompletionSource<IAccountContext>(TaskCreationOptions.RunContinuationsAsynchronously);
            var account = new Mock<IAccountContext>(MockBehavior.Strict);
            var location = source.Task.Location(cancellation.Token);
            var deactivate = source.Task.Deactivate(cancellation.Token);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WithTimeout(location));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WithTimeout(deactivate));
            source.SetResult(account.Object);
            account.Verify(c => c.Deactivate(It.IsAny<CancellationToken>()), Times.Never);

            var cancelled = Task.FromCanceled<IAccountContext>(cancellation.Token);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled.Location());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled.Deactivate());
        }

        [Theory]
        [InlineData("account")]
        [InlineData("order")]
        [InlineData("typed-order")]
        [InlineData("replacement")]
        [InlineData("profile")]
        [InlineData("renewal")]
        [InlineData("revoke")]
        [InlineData("revoke-key")]
        [InlineData("key-change")]
        public async Task ContextOperationsForwardTheTokenToTransport(string operation)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var endpoint = new Uri("https://acme.test/operation");
            var directory = new Directory(endpoint, AccountUri, endpoint, endpoint, endpoint,
                new DirectoryMeta(null, null, null, null,
                    new System.Collections.Generic.Dictionary<string, string> { ["test"] = "Test profile" }), endpoint);
            var transport = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            transport.Setup(c => c.Get<Directory>(DirectoryUri, token)).ReturnsAsync(
                new AcmeHttpResponse<Directory>(DirectoryUri, directory, null, null));
            transport.Setup(c => c.ConsumeNonce(token)).ReturnsAsync("nonce");
            transport.Setup(c => c.Post<Account>(AccountUri, It.IsAny<object>(), token)).ReturnsAsync(
                new AcmeHttpResponse<Account>(AccountUri, new Account(), null, null));

            var cancelled = new OperationCanceledException(token);
            transport.Setup(c => c.Post<Order>(endpoint, It.IsAny<object>(), token)).ThrowsAsync(cancelled);
            transport.Setup(c => c.Post<Account>(endpoint, It.IsAny<object>(), token)).ThrowsAsync(cancelled);
            transport.Setup(c => c.Post<string>(endpoint, It.IsAny<object>(), token)).ThrowsAsync(cancelled);
            transport.Setup(c => c.Get<RenewalInfo>(new Uri(endpoint + "/abc.AQ"), token)).ThrowsAsync(cancelled);
            var context = new AcmeContext(DirectoryUri, Helper.GetKeyV2(), transport.Object);
            var identifiers = new[] { "example.test" };

            if (operation == "account")
            {
                transport.Setup(c => c.Post<Account>(AccountUri, It.IsAny<object>(), token)).ThrowsAsync(cancelled);
            }

            Task pending = operation switch
            {
                "account" => context.NewAccount("test@example.test", cancellationToken: token),
                "order" => context.NewOrder(identifiers, cancellationToken: token),
                "typed-order" => context.NewOrder(new[] { new Identifier { Type = IdentifierType.Dns, Value = identifiers[0] } }, cancellationToken: token),
                "replacement" => context.NewReplacementOrder(identifiers, "abc.AQ", cancellationToken: token),
                "profile" => context.NewOrderWithProfile(identifiers, "test", cancellationToken: token),
                "renewal" => context.GetRenewalInfo("abc.AQ", token),
                "revoke" => context.RevokeCertificate(new byte[] { 1 }, RevocationReason.Unspecified, null, token),
                "revoke-key" => context.RevokeCertificate(new byte[] { 1 }, RevocationReason.Unspecified, Helper.GetKeyV2(), token),
                _ => context.ChangeKey(Helper.GetKeyV2(), token),
            };
            Assert.Same(cancelled, await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending));
        }

        [Fact]
        public async Task PublicGenerateCancelsTheRealPollingDelay()
        {
            using var cancellation = new CancellationTokenSource();
            var order = new Mock<IOrderContext>(MockBehavior.Strict);
            order.Setup(c => c.Resource(cancellation.Token)).ReturnsAsync(new Order { Status = OrderStatus.Pending });
            order.SetupGet(c => c.RetryAfter).Returns(900);
            var pending = order.Object.Generate(new CsrInfo(), Helper.GetKeyV2(), cancellationToken: cancellation.Token);
            Assert.False(pending.IsCompleted);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WithTimeout(pending));
            order.Verify(c => c.Resource(cancellation.Token), Times.Once);
        }

        [Fact]
        public async Task GenerateForwardsLiveTokenThroughDownload()
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var ready = new Order
            {
                Status = OrderStatus.Ready,
                Identifiers = new[] { new Identifier { Type = IdentifierType.Dns, Value = "example.test" } },
            };
            var order = new Mock<IOrderContext>(MockBehavior.Strict);
            order.Setup(c => c.Resource(token)).ReturnsAsync(ready);
            order.Setup(c => c.Finalize(It.IsAny<byte[]>(), token)).ReturnsAsync(new Order { Status = OrderStatus.Valid });
            var chain = new CertificateChain(System.IO.File.ReadAllText("./Data/defaultLeaf.pem"));
            order.Setup(c => c.Download("preferred", token)).ReturnsAsync(chain);
            Assert.Same(chain, await order.Object.Generate(new CsrInfo(), Helper.GetKeyV2(), "preferred", cancellationToken: token));
            order.Verify(c => c.Download("preferred", token), Times.Once);
        }

        [Fact]
        public async Task AlternateChainDownloadHonorsCancellation()
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var certificateUri = new Uri("https://acme.test/cert");
            var alternateUri = new Uri("https://acme.test/alternate");
            var transport = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            var context = new Mock<IAcmeContext>(MockBehavior.Strict);
            context.SetupGet(c => c.HttpClient).Returns(transport.Object);
            context.SetupGet(c => c.BadNonceRetryCount).Returns(1);
            context.Setup(c => c.Sign(It.IsAny<object>(), It.IsAny<Uri>(), token)).ReturnsAsync(new JwsPayload());
            transport.Setup(c => c.Post<Order>(AccountUri, It.IsAny<object>(), token)).ReturnsAsync(
                new AcmeHttpResponse<Order>(AccountUri, new Order { Certificate = certificateUri }, null, null));
            transport.Setup(c => c.Post<string>(certificateUri, It.IsAny<object>(), token)).ReturnsAsync(
                new AcmeHttpResponse<string>(certificateUri, System.IO.File.ReadAllText("./Data/defaultLeaf.pem"),
                    new[] { new { Name = "alternate", Uri = alternateUri } }.ToLookup(x => x.Name, x => x.Uri), null));
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            transport.Setup(c => c.Post<string>(alternateUri, It.IsAny<object>(), token)).Returns(async () =>
            {
                entered.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, token);
                throw new InvalidOperationException();
            });
            var pending = new OrderContext(context.Object, AccountUri).Download("UnknownRoot", token);
            await WithTimeout(entered.Task);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WithTimeout(pending));
            transport.Verify(c => c.Post<string>(alternateUri, It.IsAny<object>(), token), Times.Once);
        }

        [Fact]
        public async Task CancellingDirectoryFetchDoesNotPoisonTheCache()
        {
            var transport = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var fetched = new TaskCompletionSource<AcmeHttpResponse<Directory>>(TaskCreationOptions.RunContinuationsAsynchronously);
            transport.Setup(c => c.Get<Directory>(DirectoryUri, token)).Returns(fetched.Task);
            var context = new AcmeContext(DirectoryUri, Helper.GetKeyV2(), transport.Object);
            var pending = context.GetDirectory(token);
            cancellation.Cancel();
            fetched.SetCanceled();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            transport.Setup(c => c.Get<Directory>(DirectoryUri, default)).ReturnsAsync(
                new AcmeHttpResponse<Directory>(DirectoryUri, Helper.MockDirectoryV2, null, null));
            Assert.Same(Helper.MockDirectoryV2, await context.GetDirectory());
            Assert.Same(Helper.MockDirectoryV2, await context.GetDirectory());
            transport.Verify(c => c.Get<Directory>(DirectoryUri, default), Times.Once);
        }

        [Theory]
        [InlineData("resource")]
        [InlineData("challenge")]
        [InlineData("authorization")]
        [InlineData("account")]
        [InlineData("account-update")]
        [InlineData("finalize")]
        [InlineData("download")]
        public async Task ResourceOperationsForwardCancellation(string operation)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var transport = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            var context = new Mock<IAcmeContext>(MockBehavior.Strict);
            context.SetupGet(c => c.HttpClient).Returns(transport.Object);
            context.SetupGet(c => c.BadNonceRetryCount).Returns(1);
            context.Setup(c => c.Sign(It.IsAny<object>(), AccountUri, token)).ReturnsAsync(new JwsPayload());
            var account = new AccountContext(context.Object, AccountUri);
            context.Setup(c => c.Account(token)).ReturnsAsync(account);
            var cancelled = new OperationCanceledException(token);
            transport.Setup(c => c.Post<Account>(AccountUri, It.IsAny<object>(), token)).ThrowsAsync(cancelled);
            transport.Setup(c => c.Post<Challenge>(AccountUri, It.IsAny<object>(), token)).ThrowsAsync(cancelled);
            transport.Setup(c => c.Post<Acme.Resource.Authorization>(AccountUri, It.IsAny<object>(), token)).ThrowsAsync(cancelled);
            transport.Setup(c => c.Post<Order>(AccountUri, It.IsAny<object>(), token)).ThrowsAsync(cancelled);

            Task pending = operation switch
            {
                "resource" => account.Resource(token),
                "challenge" => new ChallengeContext(context.Object, AccountUri, ChallengeTypes.Http01, "token").Validate(token),
                "authorization" => new AuthorizationContext(context.Object, AccountUri).Deactivate(token),
                "account" => account.Deactivate(token),
                "account-update" => account.Update(cancellationToken: token),
                "finalize" => new OrderContext(context.Object, AccountUri).Finalize(new byte[] { 1 }, token),
                _ => new OrderContext(context.Object, AccountUri).Download(cancellationToken: token),
            };
            Assert.Same(cancelled, await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending));
        }

#if NET10_0_OR_GREATER
        [Fact]
        public async Task CancellationCoversResponseBodyBuffering()
        {
            using var cancellation = new CancellationTokenSource();
            var content = new BlockingContent();
            using var http = new HttpClient(new Handler((_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = content })));
            var client = new AcmeHttpClient(DirectoryUri, http);
            var pending = client.Get<string>(DirectoryUri, cancellation.Token);
            await WithTimeout(content.Started.Task);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WithTimeout(pending));
        }

        private sealed class BlockingContent : HttpContent
        {
            public TaskCompletionSource<bool> Started { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            protected override Task SerializeToStreamAsync(System.IO.Stream stream, TransportContext context)
                => throw new InvalidOperationException("The token-aware buffering overload must be used.");

            protected override async Task SerializeToStreamAsync(System.IO.Stream stream, TransportContext context, CancellationToken cancellationToken)
            {
                Started.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }
#endif

        private static async Task WithTimeout(Task task)
        {
            using var timeout = new CancellationTokenSource();
            var delay = Task.Delay(TimeSpan.FromSeconds(5), timeout.Token);
            Assert.Same(task, await Task.WhenAny(task, delay));
            timeout.Cancel();
            await task;
        }

        private sealed class Handler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send;

            public Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
            {
                this.send = send;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => send(request, cancellationToken);
        }
    }
}
