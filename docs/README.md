# Certes

Certes is an [ACME](https://en.wikipedia.org/wiki/Automated_Certificate_Management_Environment)
client for .NET, supporting ACME v2 and wildcard certificates.
It provides an easy-to-use API for managing certificates during deployment processes.

This checkout targets .NET 10, .NET 8, .NET Standard 2.0, and .NET Framework 4.6.2 for the
library. The project is being revived; see the
[repository README](https://github.com/fszlin/certes/blob/main/README.md) for
current status and [AGENTS.md](https://github.com/fszlin/certes/blob/main/AGENTS.md)
for development guidance.

## Usage

Install [Certes](https://www.nuget.org/packages/Certes/) nuget package into your project:
```PowerShell
Install-Package Certes
```
or using .NET CLI:
```DOS
dotnet add package Certes
```

[Let's Encrypt](https://letsencrypt.org/how-it-works/)
is the primary CA we supported.
It's recommend testing against
[staging environment](https://letsencrypt.org/docs/staging-environment/)
before using production environment, to avoid hitting the 
[rate limits](https://letsencrypt.org/docs/rate-limits/).

## Account

Creating new ACME account:
```C#
var acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
var account = await acme.NewAccount("admin@example.com", true);

// Save the account key for later use
var pemKey = acme.AccountKey.ToPem();
```
Use an existing ACME account:
```C#
// Load the saved account key
var accountKey = KeyFactory.FromPem(pemKey);
var acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2, accountKey);
var account = await acme.Account();
```

See [API doc](APIv2.md#accounts) for additional operations.

## Order

Place a wildcard certificate order
*(DNS validation is required for wildcard certificates)*
```C#
var order = await acme.NewOrder(new[] { "*.your.domain.name" });
```

Generate the value for DNS TXT record
```C#
var authz = (await order.Authorizations()).First();
var dnsChallenge = await authz.Dns();
var dnsTxt = acme.AccountKey.DnsTxt(dnsChallenge.Token);
```
Add a DNS TXT record to `_acme-challenge.your.domain.name` 
with `dnsTxt` value.

For non-wildcard certificate, HTTP challenge is also available
```C#
var order = await acme.NewOrder(new[] { "your.domain.name" });
```
## Authorization

Get the **token** and **key authorization string**
```C#
var authz = (await order.Authorizations()).First();
var httpChallenge = await authz.Http();
var keyAuthz = httpChallenge.KeyAuthz;
```

Save the **key authorization string** in a text file,
and upload it to `http://your.domain.name/.well-known/acme-challenge/<token>`

## Validate

Ask the ACME server to validate our domain ownership
```C#
await challenge.Validate();
```

## Certificate

Download the certificate once validation is done
```C#
var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
var cert = await order.Generate(new CsrInfo
{
    CountryName = "CA",
    State = "Ontario",
    Locality = "Toronto",
    Organization = "Certes",
    OrganizationUnit = "Dev",
    CommonName = "your.domain.name",
}, privateKey);
```

Export full chain certification
```C#
var certPem = cert.ToPem();
```

Export PFX
```C#
var pfxBuilder = cert.ToPfx(privateKey);
var pfx = pfxBuilder.Build("my-cert", "abcd1234");
```

The PFX is encrypted with AES-256 by default. For consumers that cannot read it,
such as Windows Server 2016 and earlier, set
`pfxBuilder.Encryption = PfxEncryption.Legacy` before calling `Build`.

Check the [APIs](APIv2.md) for more details.

*Historical ACME v1 documentation is available on the
[v1 branch](https://github.com/fszlin/certes/tree/v1/master).*

## CLI

The CLI is available as a dotnet global tool. This checkout requires .NET 10.
The next CLI release raises the requirement from .NET 6; install .NET 10 before
upgrading, even if your application uses the library on .NET 8.
Azure deployment commands were removed from the CLI (`az set`, `az dns`, `az app`).
Use dedicated Azure tooling for DNS/app deployment workflows.
The parser migration is complete and legacy `System.CommandLine` command paths
are no longer shipped.
For a published tool version,
check its runtime requirements on [NuGet](https://www.nuget.org/packages/dotnet-certes/).

To install Certes CLI *(you may need to restart the console session if this is the first dotnet tool installed)*
```DOS
dotnet tool install --global dotnet-certes
```

See [CLI usage](CLI.md), or simply use the `--help` option to get started
```DOS
certes --help
```

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags](https://github.com/fszlin/certes/tags) on this repository. 

Also check the [changelog](CHANGELOG.md) to see what's we are working on.

## Packages and CI status
[![NuGet](https://img.shields.io/nuget/vpre/certes.svg?label=Certes)](https://www.nuget.org/packages/certes/absoluteLatest/)
[![NuGet](https://img.shields.io/nuget/dt/certes.svg)](https://www.nuget.org/packages/certes/)
[![NuGet](https://img.shields.io/nuget/vpre/dotnet-certes.svg?label=CLI)](https://www.nuget.org/packages/dotnet-certes/absoluteLatest/)
[![NuGet](https://img.shields.io/nuget/dt/dotnet-certes.svg)](https://www.nuget.org/packages/dotnet-certes/)


Legacy CI integrations have been retired in favor of initial GitHub Actions
build/package checks and automatic offline unit tests; see the
[migration status](ci-migration.md).
