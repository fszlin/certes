
# API for ACME v2
 
This document details the API exposed for handling ACME flows, as of [draft-09][draft-09].
 
 
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

## Acounts
 
Get the url to `Terms of Service` for user to review.
 
```C#
var tos = context.TermsOfService();
```
 
Create new account.
 
```C#
var account = await context.NewAccount(
    new [] { "mailto:admin@example.com", "mailto:it@example.com" }, true);
var account = await context.NewAccount("admin@example.com", true);
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
var order = await context.Order(orderUri);
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
 
## Challenges
 
Retrieve challenges of the authorzation. 
 
```C#
var challenges = await authz.Challenges();
var dnsChallenge = await authz.Dns();
var httpChallenge = await authz.Http();
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

Let the ACME server to validate the challenge once it is ready.

```C#
await challenge.Validate();
```

## Certificates

Download certificate for a pending order.

```C#
var cert = await order.Generate(
    new CsrInfo
    {
        CountryName = "CA",
        State = "State",
        Locality = "City",
        Organization = "Dept",
    });
```

Download the certifcate for a finalized order.

```C#
var certChain = await order.Download();
```

Export the certificate to PEM, DER, or PFX.

```C#
var cert = new CertificateInfo(certChain, certKey);

var pem = cert.ToPem();
var der = cert.ToDer();
var pfx = cert.ToPfx("cert-name", "abcd1234");

var keyPem = cert.Key.ToPem();
```

Revoke certificate with account key.

```C#
context.RevokeCertificate(cert.ToDer(), RevocationReason.KeyCompromise);
```

Revoke certificate with certificate private key.

```C#
context.RevokeCertificate(cert.ToDer(), RevocationReason.KeyCompromise, certKey);
```

<!---
## Not Implemented
* Account
  * External Account Binding
-->
 
[draft-09]: https://tools.ietf.org/html/draft-ietf-acme-acme-09
