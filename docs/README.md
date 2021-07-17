# Certes

Certes is an [ACME](https://en.wikipedia.org/wiki/Automated_Certificate_Management_Environment)
client runs on .NET 4.5+ and .NET Standard 2.0+, supports ACME v2 and wildcard certificates.
It is aimed to provide an easy to use API for managing certificates during deployment processes.

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

Check the [APIs](APIv2.md) for more details.

*For ACME v1, please see [the doc here](README.v1.md).*

## CLI

The CLI is available as a dotnet global tool.
[.NET Core Runtime 2.1+](https://www.microsoft.com/net/download/dotnet-core/runtime-2.1.0)
 is required to use dotnet tools.

To install Certes CLI *(you may need to restart the console session if this is the first dotnet tool installed)*
```DOS
dotnet tool install --global dotnet-certes
```

See [CLI usage](CLI.md), or simply use the `--help` option to get started
```DOS
certes --help
```

Also check this [AppVeyor script][AppVeyorCliSample] for renewing certificates on Azure apps.

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags](https://github.com/fszlin/certes/tags) on this repository. 

Also check the [changelog](CHANGELOG.md) to see what's we are working on.

## CI Status
[![NuGet](https://img.shields.io/nuget/vpre/certes.svg?label=Certes)](https://www.nuget.org/packages/certes/absoluteLatest/)
[![NuGet](https://img.shields.io/nuget/dt/certes.svg)](https://www.nuget.org/packages/certes/)
[![NuGet](https://img.shields.io/nuget/vpre/dotnet-certes.svg?label=CLI)](https://www.nuget.org/packages/dotnet-certes/absoluteLatest/)
[![NuGet](https://img.shields.io/nuget/dt/dotnet-certes.svg)](https://www.nuget.org/packages/dotnet-certes/)


[![AppVeyor](https://img.shields.io/appveyor/ci/fszlin/certes/main.svg)](https://ci.appveyor.com/project/fszlin/certes)
[![AppVeyor](https://img.shields.io/appveyor/tests/fszlin/certes/main.svg)](https://ci.appveyor.com/project/fszlin/certes/build/tests)
[![codecov](https://codecov.io/gh/fszlin/certes/branch/main/graph/badge.svg)](https://codecov.io/gh/fszlin/certes)
[![BCH compliance](https://bettercodehub.com/edge/badge/fszlin/certes?branch=main)](https://bettercodehub.com/results/fszlin/certes)

[AppVeyorCliSample]: https://github.com/fszlin/lo0.in/blob/master/.appveyor.yml#L43
