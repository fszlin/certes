# Certes

Certes is an [ACME](https://datatracker.ietf.org/doc/html/rfc8555) client library
for .NET. It creates accounts, places orders, completes HTTP-01, DNS-01 and
TLS-ALPN-01 challenges, and exports issued certificates as PEM or PFX.

Supported targets: `net10.0`, `net8.0` and `netstandard2.0`.

## New in 4.1

- ACME Renewal Information (ARI, RFC 9773): retrieve suggested renewal windows
  and create replacement orders.
- Discover certificate profiles and select a profile when creating an order.
- Order certificates for IPv4 and IPv6 addresses (RFC 8738), with IP subject
  alternative names in CSRs and TLS-ALPN validation certificates.

These features require support from the ACME server. See the
[API guide](https://github.com/fszlin/certes/blob/main/docs/APIv2.md) for usage
and challenge requirements.

## Getting started

```csharp
var acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
var account = await acme.NewAccount("admin@example.com", true);
var order = await acme.NewOrder(new[] { "example.com" });
```

Test against a staging CA before using a production CA.

## Documentation

- [Usage guide](https://github.com/fszlin/certes/blob/main/docs/README.md)
- [API reference](https://github.com/fszlin/certes/blob/main/docs/APIv2.md)
- [Changelog and breaking changes](https://github.com/fszlin/certes/blob/main/docs/CHANGELOG.md)
- [Issues](https://github.com/fszlin/certes/issues)

The `dotnet-certes` package provides a command-line tool built on this library.

Licensed under the [MIT license](https://github.com/fszlin/certes/blob/main/LICENSE).
