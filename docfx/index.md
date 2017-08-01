# Certes ACME Client

[![NuGet](https://buildstats.info/nuget/certes)](https://www.nuget.org/packages/certes/)
[![MyGet](https://buildstats.info/myget/dymetis/certes?includePreReleases=true)](https://www.myget.org/feed/dymetis/package/nuget/certes)

[![Travis](https://travis-ci.org/fszlin/certes.svg?branch=master)](https://travis-ci.org/fszlin/certes)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/4wwiivqs8rl0l63q/branch/master?svg=true)](https://ci.appveyor.com/project/fszlin/certes/branch/master)
[![codecov](https://codecov.io/gh/fszlin/certes/branch/master/graph/badge.svg)](https://codecov.io/gh/fszlin/certes)

Certes is a client implantation for the Automated Certificate Management
Environment (ACME) protocol, build on .NET Core. It is aimed to provide a easy
to use API for managing certificates using scripts during build process.

Before [Let's Encrypt](https://letsencrypt.org), SSL/TLS certificate for HTTPS
was a privilege for who can afford it. With Certes, you can quickly generate
certificates using .NET or command line, and it is **free**.

# Get Certified in 5 Minutes

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
    certes cert --name mycert --distinguished-name "CN=CA, ST=Ontario, L=Toronto, O=Certes, OU=Dev, CN=my_domain.com" #--v www.my_domain.com --v my_domain2.com

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

# Get Started

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
    var httpChallengeInfo = authz.Data.Challenges.Where(c => c.Type == ChallengeTypes.Http01).First();
    var keyAuthString = client.ComputeKeyAuthorization(httpChallengeInfo);
    
    // Do something to fullfill the challenge,
    // e.g. upload key auth string to well known path, or make changes to DNS

    // Info ACME server to validate the identifier
    var httpChallenge = await client.CompleteChallenge(httpChallengeInfo);

    // Check authorization status
    authz = await client.GetAuthorization(httpChallenge.Location);
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