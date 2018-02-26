# Certes [![Twitter URL](https://img.shields.io/twitter/url/http/shields.io.svg?style=social)][tw]

Certes is an [ACME](https://en.wikipedia.org/wiki/Automated_Certificate_Management_Environment)
client runs on .NET 4.5+ and .NET Standard 1.3+, supports ACME v2 and wildcard certificates.
It is aimed to provide an easy to use API for managing certificates during deployment processes.

**Util Let's Encrypt releases [v2 endpoint](https://community.letsencrypt.org/t/acmev2-and-wildcard-launch-delay/53654),
please continue to use [v1 API](https://github.com/fszlin/certes/blob/master/docs/README.v1.md) for production.**

## Usage

Install [Certes](https://www.nuget.org/packages/Certes/) nuget package into your project:
```
Install-Package Certes
```
or using .NET CLI:
```
dotnet add package Certes
```

Creating new ACME account:
```C#
var acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
var account = acme.NewAccount("admin@example.com", true);
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
await httpChallenge.Validate();
```

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

Export PFX
```C#
var pfxBuilder = cert.ToPfx(privateKey);
var pfx = pfxBuilder.Build("my-cert", "abcd1234");
```

Check the [APIs](APIv2.md) for more details.

*For ACME v1, please see [the doc here](README.v1.md).*

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/fszlin/certes/tags). 

Also check the [changelog](CHANGELOG.md) to see what's we are working on.

## CI Status
[![NuGet](https://img.shields.io/nuget/vpre/certes.svg)](https://www.nuget.org/packages/certes/absoluteLatest/)
[![NuGet](https://img.shields.io/nuget/dt/certes.svg)](https://www.nuget.org/packages/certes/)
[![AppVeyor](https://img.shields.io/appveyor/ci/fszlin/certes/master.svg)](https://ci.appveyor.com/project/fszlin/certes)
[![AppVeyor](https://img.shields.io/appveyor/tests/fszlin/certes/master.svg)](https://ci.appveyor.com/project/fszlin/certes/build/tests)
[![codecov](https://codecov.io/gh/fszlin/certes/branch/master/graph/badge.svg)](https://codecov.io/gh/fszlin/certes)
[![BCH compliance](https://bettercodehub.com/edge/badge/fszlin/certes?branch=master)](https://bettercodehub.com/results/fszlin/certes)

[tw]: https://twitter.com/share?url=https%3A%2F%2Fgithub.com%2Ffszlin%2Fcertes&via=certes_acme&related=fszlin&hashtags=certes%2Cssl%2Clets-encrypt%2Cacme%2Chttps&text=get%20free%20SSL%20via%20certes
