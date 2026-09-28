# Upgrading to v5 (development)

v5 is under development; these changes are not in the released v4.1.0 package.

## Cancellation API change

Core asynchronous interfaces, implementations, and convenience extensions now
accept a final optional `CancellationToken cancellationToken = default`.
Existing parameterless/tokenless overloads are replaced, not retained.

Most ordinary calls continue to compile without changes:

```csharp
var account = await acme.Account();
```

Pass a token explicitly to cancel an operation:

```csharp
using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
var order = await acme.NewOrder(
    new[] { "example.com" }, cancellationToken: cancellation.Token);
var authorizations = await order.Authorizations(cancellation.Token);
// Provision and validate challenges before generating the certificate.
var chain = await order.Generate(csrInfo, certificateKey,
    cancellationToken: cancellation.Token);
```

The token flows through directory/account discovery, nonce acquisition, HTTP
requests, bad-nonce retries, authorization lookups, pagination, alternate-chain
downloads, and polling delays (including server-directed `Retry-After`).
Cancellation is cooperative: it does not interrupt synchronous cryptographic
operations. `OperationCanceledException` (including `TaskCanceledException`)
propagates without being translated into an ACME protocol error.

Cancellation does not roll back requests already accepted by the CA. An account,
order, revocation, or key-change request may have taken effect even if the caller
observes cancellation while the request is in flight. Reconcile server state
before retrying state-changing work; an order URL lost in flight may not be
recoverable if the CA does not support listing orders.

Once the transport returns a complete response, a late cancellation does not
discard it: account/order creation returns the location, finalization returns its
order resource, and a successful key change updates the local account key.
Terminal ACME errors are likewise preserved. Cancellation is checked before
sending and before another retry, poll, page, or download request. Multi-step
operations such as `Generate` may still cancel before the next step; keep the
order context to resume or inspect the order afterward.
Tokens apply to individual operations, not the lifetime of the reusable context.
Pre-cancelled account/directory lookups cancel even when the value is cached;
cancelled fetches do not cache a cancelled task.

### Compatibility and migration

- **Recompile consumers.** Adding an optional parameter changes the CLR method
  signature and is a binary breaking change.
- **Update custom implementations**, including `IAcmeContext`, `IAcmeHttpClient`,
  and resource-context interfaces. Forward the token through your asynchronous
  work and preserve cancellation exceptions. Derived resource interfaces inherit
  the updated `Resource` signature.
- **Update mocks and callbacks.** Mock callbacks accepting method arguments must
  include the additional token. Use explicit token arguments in expression-tree
  setups for compatibility with compilers that reject omitted optional arguments.
- **Update method groups** whose delegate types require the old arity; use a
  lambda passing `CancellationToken.None` or change the delegate signature.
- **Task-based account helpers:** `accountTask.Location(token)` and
  `accountTask.Deactivate(token)` cancel their wait for the supplied task; they
  cannot cancel a task already started without that token. Also pass the token
  to `acme.Account(token)`. Deactivation is not started after a cancelled wait.
  The caller retains ownership of the supplied task and should observe its eventual
  result or exception, even after cancelling the helper's wait.
- Synchronous methods such as `Order(Uri)`, key factories, and certificate export
  retain their signatures. Library target frameworks and runtime dependency
  minimums are unchanged by this API migration.

The Spectre CLI migration and command-line cancellation wiring are tracked
separately in [#405](https://github.com/fszlin/certes/issues/405).
