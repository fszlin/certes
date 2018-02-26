# Certes ACME Client

[![NuGet](https://img.shields.io/nuget/v/certes.svg)](https://www.nuget.org/packages/certes/)
[![NuGet](https://img.shields.io/nuget/dt/certes.svg)](https://www.nuget.org/packages/certes/)

[![Travis](https://img.shields.io/travis/fszlin/certes/master.svg)](https://travis-ci.org/fszlin/certes)
[![AppVeyor](https://img.shields.io/appveyor/ci/fszlin/certes/master.svg)](https://ci.appveyor.com/project/fszlin/certes)
[![AppVeyor](https://img.shields.io/appveyor/tests/fszlin/certes/master.svg)](https://ci.appveyor.com/project/fszlin/certes)
[![codecov](https://codecov.io/gh/fszlin/certes/branch/master/graph/badge.svg)](https://codecov.io/gh/fszlin/certes)

Certes is a client implementation for the Automated Certificate Management
Environment (ACME) protocol, build on .NET Core. It is aimed to provide a easy
to use API for managing certificates using scripts during build process.

**ACME v2** with wildcard SSL is [around the corner](https://community.letsencrypt.org/t/staging-endpoint-for-acme-v2/49605) now,
check out our [v2 API](README.v1.md)!

## Get Certified in 5 Minutes

Install [.NET Core](https://www.microsoft.com/net/core)

Download the [latest release](https://github.com/fszlin/certes/releases), 
   and extract the files

Run these commands to start the authorization process

```Bash
    # Create new registration on LE, and accept terms of services
    certes register --email your_email@my_domain.com --agree-tos

    # Initialize authorization for host name(s)
    certes authz --v my_domain.com #--v www.my_domain.com --v my_domain2.com

    # Show the http-01 key authorization for specified host name(s)
    certes authz --key-authz http-01 --v my_domain.com #--v www.my_domain.com --v my_domain2.com
```

Make changes to your site so that it serves the **key authorization string** 
   on the well know path.
  * The **key authorization string** consists of the token and the thumbprint
    of the registration key, in form of `<token>.<thumbprint>`
  * You can simply save the **key authorization string** in a text file, and
    upload it to `http://my_domain.com/.well-known/acme-challenge/<token>`
  * For testing purposes, if you are hosting an ASP.NET Core app, you can add
    the following to ```Configure``` method of ```Startup``` class

```C#
        app.Map("/.well-known/acme-challenge", sub =>
        {
            sub.Run(async context =>
            {
                var path = context.Request.Path.ToUriComponent();
                if (path?.Length > 1 && path.StartsWith("/"))
                {
                    context.Response.ContentType = "plain/text";
                    await context.Response.WriteAsync($"{path.Substring(1)}.<thumbprint>");
                }
            });
        });
```

  * For more details, see [section 8 of the ACME spec](https://tools.ietf.org/html/draft-ietf-acme-acme-07#section-8)

Continue the authorization process and generate the certificate

```Bash
    # Complete the http-01 challenge
    certes authz --complete-authz http-01 --v my_domain.com #--v www.my_domain.com --v my_domain2.com

    # Check the challenge status, wait until it becomes "valid"
    certes authz --refresh http-01 --v my_domain.com #--v www.my_domain.com --v my_domain2.com

    # Create a certificate with the distinguished name, and additional SAN names
    certes cert --name mycert --distinguished-name "C=CA, ST=Ontario, L=Toronto, O=Certes, OU=Dev, CN=my_domain.com" #--v www.my_domain.com --v my_domain2.com

    # Export the certificate in DER
    certes cert --name mycert --export-cer ./mycert.cer

    # Export the certificate's private key in PEM
    certes cert --name mycert --export-key ./mycert.key

    # Export the certificate with private key in PFX
    certes cert --name mycert --export-pfx ./mycert.pfx --password abcd1234

    # Revoke the certificate
    certes cert --name mycert --revoke
```

Install the certificate on your host server.

More...
  * Append ```--server https://acme-staging.api.letsencrypt.org/directory``` to the commands
    for testing again LE staging server.
  * By default, the account and contextual data are saved in **data.json**, 
    use ```--path``` option to change the location.

## Get Started

You can get Certes by grabbing the latest
[NuGet package](https://www.nuget.org/packages/Certes).

```C#
using (var client = new AcmeClient(WellKnownServers.LetsEncrypt))
{
    // Create new registration
    var account = await client.NewRegistraton("mailto:test@example.com");

    // Accept terms of services
    account.Data.Agreement = account.GetTermsOfServiceUri();
    account = await client.UpdateRegistration(account);

    // Initialize authorization
    var authz = await client.NewAuthorization(new AuthorizationIdentifier
    {
        Type = AuthorizationIdentifierTypes.Dns,
        Value = "www.my_domain.com"
    });

    // Comptue key authorization for http-01
    var httpChallengeInfo = authz.Data.Challenges.First(c => c.Type == ChallengeTypes.Http01);
    var keyAuthString = client.ComputeKeyAuthorization(httpChallengeInfo);

    // Or compute DNS reccord TXT value
    var dnsChallengeInfo = authz.Data.Challenges.First(c => c.Type == ChallengeTypes.Dns01);
    var dnsTxtValue = client.ComputeDnsValue(dnsChallengeInfo);
    
    // Do something to fullfill the challenge,
    // e.g. upload key auth string to well known path, or make changes to DNS

    // Info ACME server to validate the identifier
    var httpChallenge = await client.CompleteChallenge(httpChallengeInfo);
    var dnsChallenge = await client.CompleteChallenge(dnsChallengeInfo);

    // Check authorization status (use the proper challenge to check Authorization State)
    authz = await client.GetAuthorization(httpChallenge.Location); // or dnsChallenge.Location
    while (authz.Data.Status == EntityStatus.Pending)
    {
        // Wait for ACME server to validate the identifier
        await Task.Delay(10000);
        authz = await client.GetAuthorization(httpChallenge.Location);
    }

    if (authz.Data.Status == EntityStatus.Valid)
    {
        // Create certificate
        var csr = new CertificationRequestBuilder();
        csr.AddName("CN", "www.my_domain.com");
        var cert = await client.NewCertificate(csr);

        // Export Pfx
        var pfxBuilder = cert.ToPfx();
        var pfx = pfxBuilder.Build("my-free-cert", "abcd1234");
        File.WriteAllBytes("./my-free-cert.pfx", pfx);

        // Revoke certificate
        await client.RevokeCertificate(cert);
    }
}
```

Read more about the [supported ACME flows](implementation-status.md).

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/fszlin/certes/tags). 

Also check the [changelog](CHANGELOG.md) to see what's we are working on.
