
# API for ACME v2
 
This document details the API exposed for handling ACME flows, as of [draft-12][draft].
 
 
## Initialization
 
Create the context with specific ACME server by providing the directory URI.
 
```C#
var context = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
```
 
Use specific key for existing account or creating new account.
 
```C#
var context = new AcmeContext(
    WellKnownServers.LetsEncryptStagingV2,
    KeyFactory.FromPem("account-key.pem"));
```
 
Export the account key for later use.

```C#
var pem = context.AccountKey.ToPem();
var der = context.AccountKey.ToDer();
```

## Accounts
 
Get the url to `Terms of Service` for user to review.
 
```C#
var tos = context.TermsOfService();
```
 
Create new account.
 
```C#
var account = await context.NewAccount(
    new [] { "mailto:admin@example.com", "mailto:it@example.com" }, true);
var account = await context.NewAccount("admin@example.com", true);

// external account binding
var account = await context.NewAccount("admin@example.com", true, "(EAB Key Identifier)","(EAB Key)");
var account = await context.NewAccount("admin@example.com", true, "(EAB Key Identifier)","(EAB Key)","(EAB Key Algorithm e.g.HS256)");
```
 
Fetch existing account from server.
 
```C#
var account = await context.Account();
```
 
Fetch the account info from server.
 
```C#
var accountInfo = await account.Resource();
```
 
Update contacts, or accept `Terms of Service` again if it's updated.
 
```C#
await account.UpdateUpdate(
    contact: new[] { $"mailto:support@example.com" },
    agreeTermsOfService: true);
```
 
Update the account key.
 
```C#
var newKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
await account.ChangeKey(newKey);
 
File.WriteAllText("new-key.pem", newKey.ToPem());
```
 
Deactivate account.
```C#
await account.Deactivate();
```
 
<!---
Navigate to related entities.
```C#
var orders = await account.Orders();
```
-->
 
## Orders
 
Apply for certificate issuance.
 
```C#
var order = await context.NewOrder(new [] { "*.example.com" });
var orderUri = order.Location;
```

Retrieve order by URI.
 
```C#
var order = context.Order(orderUri);
```

Finalize the order.

```C#
var certKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
await orderCtx.Finalize(
    new CsrInfo
    {
        CountryName = "CA",
        State = "State",
        Locality = "City",
        Organization = "Dept",
    }, certKey);
```

Send customized CSR to finalize the order.

```C#
var csr = new CertificationRequestBuilder();
csr.AddName($"C=CA, ST=State, L=City, O=Dept, CN=*.example.com");

await orderCtx.Finalize(csr.Generate());
```

Download the certificate PEM.

```C#
var certChain = await order.Download();
```

Download the certificate PEM signed with a specific root certificate

```C#
var certChain = await order.Download("ISRG X1 Root");
```

Finalize and download the certificate.

```C#
var certKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
var cert = await order.Generate(
    new CsrInfo
    {
        CountryName = "CA",
        State = "State",
        Locality = "City",
        Organization = "Dept",
    }, certKey);
```

Finalize and download the certificate signed with a specific root certificate.

```C#
var certKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
var cert = await order.Generate(
    new CsrInfo
    {
        CountryName = "CA",
        State = "State",
        Locality = "City",
        Organization = "Dept",
    }, certKey, "ISRG X1 Root");
```


### Certificate profiles

Servers supporting the [ACME profiles extension (Internet-Draft)](https://datatracker.ietf.org/doc/draft-ietf-acme-profiles/)
advertise available profiles in directory metadata. Profile descriptions are
strings: they can contain prose or a documentation URL.

```C#
var directory = await context.GetDirectory();
var profiles = directory.Meta?.Profiles;
if (profiles != null && profiles.ContainsKey("tlsserver"))
{
    var order = await context.NewOrderWithProfile(new[] { "your.domain.name" }, "tlsserver");
    // Validate authorizations and generate the certificate as usual.
    var selectedProfile = (await order.Resource()).Profile;
}
```

Names are case-sensitive and server-specific; Certes does not hard-code any CA's
profiles. `NewOrderWithProfile` throws `NotSupportedException` when profiles are
not advertised and `ArgumentException` for empty or unadvertised names, rather
than silently falling back. A server may still reject an advertised profile for
an account or requested identifier; its ACME problem details are preserved in
`AcmeRequestException`. The directory is cached by the context; use a new context
when refreshed profile discovery is needed.

To send a name the directory does not list, such as a private profile agreed
with the CA, pass `allowUnadvertisedProfile: true`. The server must still
advertise profile support, and it may reject the name with `invalidProfile`.

Use the optional `replacedCertificateId` parameter to combine profile selection
with [ARI replacement orders](#renewal-information-ari):

```C#
var order = await context.NewOrderWithProfile(new[] { "your.domain.name" }, "tlsserver",
    replacedCertificateId: previousChain.GetRenewalInfoCertificateId());
```

Existing `NewOrder` and `NewReplacementOrder` calls omit `profile`, leaving the
choice to the CA. A profile can affect certificate lifetime and permitted CSR
fields; consult the CA's description before selecting one.

### IP address identifiers

Servers supporting [RFC 8738](https://www.rfc-editor.org/rfc/rfc8738) can issue
certificates for IP addresses. The string overloads of `NewOrder`,
`NewOrderWithProfile`, and `NewReplacementOrder` detect IP addresses: values that
strictly parse as an IP address are sent as `ip` identifiers, and everything else
as `dns`. To set the type explicitly, pass `Identifier` values instead.

```C#
// Let's Encrypt issues IP address certificates only under its shortlived profile.
var order = await context.NewOrderWithProfile(new[] { "203.0.113.10", "your.domain.name" }, "shortlived");

// Equivalent, with explicit types:
var identifiers = new[]
{
    new Identifier { Type = IdentifierType.Ip, Value = "203.0.113.10" },
    new Identifier { Type = IdentifierType.Dns, Value = "your.domain.name" },
};

order = await context.NewOrderWithProfile(identifiers, "shortlived");
var authz = await order.Authorization("203.0.113.10", IdentifierType.Ip);
```

- IPv4 addresses must be dotted-quad without leading zeros; IPv6 addresses must
  not include a zone ID. Values are sent in canonical form (RFC 5952 for IPv6), and
  invalid typed IP values throw `ArgumentException` before any request is made.
  With the string overloads, values that are not valid IP addresses (such as
  `01.2.3.4` or `fe80::1%eth0`) are sent as `dns` identifiers.
- `Authorization(value, IdentifierType.Ip)` matches by address, so any valid
  spelling of the address finds the authorization.
- Use `http-01` or `tls-alpn-01`; `dns-01` cannot validate IP addresses. For
  `http-01`, serve the key authorization on port 80 of the address. For
  `tls-alpn-01`, the server connects to the address using a reverse-DNS SNI name
  (for example `10.113.0.203.in-addr.arpa`), and `TlsAlpnCertificate` should be
  passed the IP address so the certificate carries an IP SAN.
- CSRs from `CreateCsr`, `Finalize`, and `Generate` encode IP addresses as IP SANs.
  When `CsrInfo.CommonName` is not set, the common name is the first DNS identifier of
  at most 64 characters; IP addresses are never used, and an IP-only CSR has no
  common name.

## Authorizations
 
Retrieve authorizations of the order.
 
```C#
var authorizations = await order.Authorizations();
```
 
Search authorization by domain name.
 
```C#
var authz = await order.Authorization("*.example.com");
var authzUri = authz.Location;
```

Retrieve authorization by URI.
 
```C#
var authz = await context.Authorization(authzUri);
```
 
## Persistent DNS authorization (v5 development)

Certes supports the interactive challenge flow from
[draft-ietf-acme-dns-persist-02](https://www.ietf.org/archive/id/draft-ietf-acme-dns-persist-02.html).
This is an evolving draft, not an RFC. It uses a domain-bound hashed account URI,
the challenge's `issuerDomainNames`, and directory `meta.accountHashPrefix`.

```C#
var authz = await order.Authorization("example.com", cancellationToken: cancellationToken);
var authorization = await authz.Resource(cancellationToken);
if (authorization.Status != AuthorizationStatus.Valid)
{
    var challenge = await authz.DnsPersist(cancellationToken)
        ?? throw new NotSupportedException("The server did not offer dns-persist-01.");
    var record = await context.GetDnsPersistRecord(
        authorization, await challenge.Resource(cancellationToken),
        persistUntil: DateTimeOffset.UtcNow.AddDays(30),
        cancellationToken: cancellationToken);

    // Publish one TXT record at record.Name with record.Value, then wait for DNS propagation.
    // For zone files, use record.Name + ". IN TXT " + record.ZoneFileValue.
    await challenge.Validate(cancellationToken);
    // Poll the authorization until valid before finalizing the order.
}
```

`GetDnsPersistRecord` uses the current account key and account location, and checks
the challenge's issuer list against available directory issuer/CAA identities.
The default issuer is the first challenge identity; select another with
`issuerDomainName`. The record contains a SHA-256 hash binding the normalized DNS
name, account public-key thumbprint, and exact account URL. It contains neither a
per-challenge token nor a plaintext account URL. IP identifiers are rejected.

For a wildcard authorization, explicitly pass `wildcardPolicy: true`. That adds
`policy=wildcard`, authorizing the base domain **and its wildcards and subdomains**;
it is never enabled implicitly. Without it, only the specific DNS name is authorized.
`persistUntil` is optional; omitted records have no explicit expiration. Past
expirations are rejected. Keep a successfully provisioned record for subsequent
renewals, and remove it when that persistent authorization is no longer wanted.

`record.Value` is the raw value for DNS APIs. `record.TextChunks` splits it into
ASCII strings of at most 255 octets; these belong to **one** TXT record, not separate
records. `record.ZoneFileValue` supplies escaped, quoted chunks for a zone file.
Certes generates the record but does not publish DNS, monitor expiration, or
implement delegated pre-provisioning. Those workflows must implement the draft's
account proof and provisioning requirements separately.

`IChallengeContext.Token` is null for this challenge, and `KeyAuthz` throws
`InvalidOperationException`. Use the record helper instead. `Validate` checks
the current challenge and directory metadata before posting `{}`.

**Interoperability limit:** Pebble 2.10.1 implements an older draft with
`issuer-domain-names` and plaintext account URIs; Certes rejects it rather than
silently generating an old-format record. On September 28, 2026, the Let's Encrypt
staging directory did not advertise the -02 metadata. Offline tests check the
draft's published hash vector and wire behavior; the pinned Pebble test checks
legacy-draft rejection, not successful DNS-PERSIST-01 issuance. Public-CA
interoperability has not been verified.

## Challenges
 
Retrieve challenges of the authorzation. 
 
```C#
var challenges = await authz.Challenges();
var dnsChallenge = await authz.Dns();
var httpChallenge = await authz.Http();
var tlsAlpnChallenge = await authz.TlsAlpn();
```
 
Create the respone file for provisioning to `/.well-know/acme-challenge/`.
 
```C#
var keyAuth = httpChallenge.KeyAuthz;
File.WriteAllText(httpChallenge.Token, keyAuth);
```

Compute the value for DNS TXT record.

```C#
var dnsTxt = context.AccountKey.DnsTxt(challenge.Token);
```

Generate certificate with X509 ACME validation extension.

```C#
var alpnCertKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
var alpnCert = context.AccountKey.TlsAlpnCertificate(challenge.Token, "www.my-domain.com", alpnCertKey);
```

Let the ACME server to validate the challenge once it is ready.

```C#
await challenge.Validate();
```

## Certificates

Download certificate for a pending order.

```C#
var certKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
var certChain = await order.Generate(
    new CsrInfo
    {
        CountryName = "CA",
        State = "State",
        Locality = "City",
        Organization = "Dept",
    },
    certKey);
```

Download the certifcate for a finalized order.

```C#
var certChain = await order.Download();
```

Export the certificate to PEM, DER, or PFX.

```C#
var pem = certChain.ToPem();                // certificate and issuers
var pemWithKey = certChain.ToPem(certKey);  // private key first, then certificates
var der = certChain.Certificate.ToDer();    // leaf certificate only
var pfx = certChain.ToPfx(certKey).Build("cert-name", "abcd1234");

var keyPem = certKey.ToPem();
```

Revoke certificate with account key.

```C#
await context.RevokeCertificate(certChain.Certificate.ToDer(), RevocationReason.KeyCompromise);
```

Revoke certificate with certificate private key.

```C#
await context.RevokeCertificate(certChain.Certificate.ToDer(), RevocationReason.KeyCompromise, certKey);
```

## Renewal Information (ARI)

When the server supports [RFC 9773](https://www.rfc-editor.org/rfc/rfc9773)
(its directory lists `renewalInfo`), ask it when to renew a certificate. The
certificate identifier is derived from the certificate's authority key identifier
and serial number.

```C#
var certChain = new CertificateChain(savedPem); // or the chain from order.Generate()
var certId = certChain.GetRenewalInfoCertificateId();

var info = await context.GetRenewalInfo(certId);
// Renew at a random time within info.SuggestedWindow.Start..End.
// Bound info.RetryAfter before scheduling the next poll (see below).
```

The application schedules renewal-info polling. `RetryAfter` exposes the positive
delay reported by the HTTP client without clamping it. Per RFC 9773 section 4.3.2,
callers must set reasonable limits on the checking interval, for example **one
minute to one day**. When no delay is available (`null`), use a locally configured
fallback, such as **six hours**. Error backoff takes priority over this interval.
Stop polling after the certificate expires or is considered replaced.

When renewing, create the order with `NewReplacementOrder` so the server knows
which certificate it replaces, then proceed as usual.

```C#
var order = await context.NewReplacementOrder(new[] { "your.domain.name" }, certId);
```

`GetRenewalInfo` throws `NotSupportedException` when the directory has no
`renewalInfo` endpoint. The request is an unauthenticated GET and does not
require an account.

A missing suggested window, or one whose end is at or before its start, causes
`GetRenewalInfo` to throw `AcmeException`. Treat that as a failed renewal-info
request: use a fallback renewal schedule and retry after the locally configured
default interval (RFC 9773 section 4.3.3). A valid window entirely in the past is
accepted; if the chosen renewal time is already past, attempt renewal immediately,
subject to error backoff.

<!---
## Not Implemented
* Account
  * External Account Binding
-->
 
[draft]: https://tools.ietf.org/html/draft-ietf-acme-acme-12
