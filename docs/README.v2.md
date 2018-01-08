# Certes

Certes is a [ACME](https://en.wikipedia.org/wiki/Automated_Certificate_Management_Environment)
client runs on .NET 4.5+ and .NET Standard 1.3+. It is aimed to provide an easy
to use API for managing certificates during deployment processes.

## Usage

Install [Certes](https://www.nuget.org/packages/Certes/) nuget package into your project:
```
Install-Package Certes -IncludePrerelease
```
or using .NET CLI:
```
dotnet add package Certes -v 2.0.0-*
```

Creating a new ACME account:
```C#
var acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
var account = acme.NewAccount("admin@example.com");
```

Place an order for certificate
```C#
var order = await acme.NewOrder(new[] { "your.domain.name" });
```

Get the **token** and **key authorization string**
```C#
var authz = (await order.Authorizations()).First();
var httpChallenge = await authz.Http();
var keyAuthz = httpChallenge.KeyAuthz;
```

Prepare for http challenge by saving the **key authorization string** 
in a text file, and upload it to `http://your.domain.name/.well-known/acme-challenge/<token>`

Ask the ACME server to validate our domain ownership
```C#
// TODO: search the order/authz - https://github.com/letsencrypt/boulder/issues/3335
await httpChallenge.Validate();
```

Send CSR for our certificate
```C#
var certKey = DSA.NewKey(SignatureAlgorithm.RS256);
await order.Finalize(new CsrInfo
{
    CountryName = "CA",
    State = "Ontario",
    Locality = "Toronto",
    Organization = "Certes",
    OrganizationUnit = "Dev",
    CommonName = "your.domain.name",
}, certKey);
```

Download our fresh certificate
```C#
var pem = await order.Download();
```

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/fszlin/certes/tags). 

Also check the [changelog](CHANGELOG.md) to see what's we are working on.

## CI Status
[![NuGet](https://img.shields.io/nuget/v/certes.svg)](https://www.nuget.org/packages/certes/)
[![NuGet](https://img.shields.io/nuget/dt/certes.svg)](https://www.nuget.org/packages/certes/)
[![AppVeyor](https://img.shields.io/appveyor/ci/fszlin/certes.svg)](https://ci.appveyor.com/project/fszlin/certes)
[![AppVeyor](https://img.shields.io/appveyor/tests/fszlin/certes.svg)](https://ci.appveyor.com/project/fszlin/certes)
[![codecov](https://codecov.io/gh/fszlin/certes/branch/master/graph/badge.svg)](https://codecov.io/gh/fszlin/certes)
