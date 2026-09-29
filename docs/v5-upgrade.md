# Upgrading to v5 (prerelease)

These changes are included in `5.0.0-beta.1`, the first v5 prerelease, and are not
in the stable v4.1.0 package. DNS-PERSIST-01 is not included in this beta.

## Persistent DNS authorization

The library and CLI add experimental DNS-PERSIST-01 support for
`draft-ietf-acme-dns-persist-02`. See the [API guide](APIv2.md#persistent-dns-authorization-v5-development)
for record generation, explicit wildcard policy, and server compatibility limits.
This draft uses hashed account URIs and is incompatible with pinned Pebble 2.10.1's
older cleartext-account format. Current-draft issuance has not been verified.

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

## CLI cancellation and Spectre migration

The v5 CLI uses Spectre.Console.Cli 0.55.0 and asynchronous command dispatch.
Command names, options, and JSON result shapes are retained. Ctrl+C requests
cooperative cancellation of the active command, including settings/key reads,
HTTP requests, authorization lookups, order-list pages, and certificate downloads.
The console handler is removed when the invocation finishes. The first Ctrl+C
requests cancellation; a second allows immediate process termination. SIGTERM
is not handled cooperatively in this version.

Exit codes are **0** for success, **1** for parsing/operation errors, and **130**
when the invocation is cancelled. An HTTP timeout or unrelated cancellation is
an operation error (1), not a user cancellation. Cancellation prints
`Operation cancelled.` without a stack trace. Synchronous key generation and
PFX construction are not interruptible mid-operation.

With an explicit `--out` path, `account new` saves its account key before sending
the creation request, and `order finalize` saves a newly generated certificate key
before sending the CSR. A write failure or cancellation before saving prevents
the request. The file remains if the request later fails or is cancelled; it may
contain a key the CA never accepted. Existing output files are atomically replaced
before sending, so use a dedicated path and retain the saved key for retries.

Without `--out`, a newly generated account key is saved to user settings only after
creation succeeds, to avoid overwriting an existing account key on a failed request.
That post-success write deliberately ignores cancellation. A generated certificate
key destined for stdout is likewise emitted only after finalization succeeds.
These two paths can still lose a generated key if cancellation interrupts an
in-flight request the CA accepted. For recoverability, use `--out` or supply an
already-persisted key. Force termination can interrupt persistence too.

File writes remain atomic and retain owner-only Unix permissions.
Ordinary cancellable writes stop before replacement and clean up their temporary
file. A completed replacement or successful finalization can still report success
if cancellation arrives too late to stop it.

If account/order creation succeeds but cancellation prevents the follow-up resource
lookup, the CLI emits JSON containing the returned `location` and exits 130.
Retain this URL to inspect the account/order later. A request cancelled before a
response arrives still has the in-flight uncertainty described above.
