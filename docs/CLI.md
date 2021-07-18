
# Free certificates using Certes CLI

[Certes CLI](https://www.nuget.org/packages/dotnet-certes/)
is delivered as a `dotnet` global tool, and it can be install
using [dotnet tool](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install)
command:

```PowerShell
dotnet tool install --global dotnet-certes
```

## Managing ACME account

An ACME account is needed for generating SSL certificates. If you don't
have one already, you can register a new account:

```PowerShell
certes account new email@example.com
```

To use an existing account, simply import your account key:

```PowerShell
certes account set ./account-key.pem
```

You may review the current account:

```PowerShell
certes account show
```

The result should look similar to this:

```json
{
  "location": "https://acme-v02.api.letsencrypt.org/acme/acct/1",
  "resource": {
    "status": "valid",
    "contact": [
      "mailto:email@example.com"
    ]
  }

```

## Ordering SSL Certificates

With an `valid` ACME account, we can start generating SSL certificates now.

> You may add up to 100 domains in one order, and mixing wildcard and non-wildcard
> domains, as long as the domains don't overlap with each other.

```PowerShell
certes order new *.example.com api.example.net
```

Keep note of the order location, which we will use it in the next steps:

```json
{
  "location": "https://acme-v02.api.letsencrypt.org/acme/order/2/3",
  "resource": {
    "status": "pending",
    "expires": "2018-07-03T04:55:04+00:00",
    "identifiers": [
      {
        "type": "dns",
        "value": "*.example.com"
      },
      {
        "type": "dns",
        "value": "api.example.net"
      }
    ],
    "authorizations": [
      "https://acme-v02.api.letsencrypt.org/acme/authz/coHQk9WEhTHTjd9eWFeA2UueKuG8qjBKP3EyVdQXZsk",
      "https://acme-v02.api.letsencrypt.org/acme/authz/E1MtjxAiM1l_TyK3OWhMR1n9-u3DYOkUVxchzmZ2OaU"
    ],
    "finalize": "https://acme-v02.api.letsencrypt.org/acme/finalize/2/3"
  }
}
```

## Validating Domain Ownership

We will need to prove that we have control of the domains
we claimed in the order, so the ACME server would issue 
the SSL certificate. The ACME server may send varies challenges
for each domain, such as `DNS`, `HTTP`, and `TLS-ALPN`, and we
can fullfill any one of them.

> For wildcard domains, currently only `DNS` challenge is accepted.

### Setup for Challenges

Certes CLI provides commands for generating necessary data to fullfill
the challenges.

#### DNS challenge
 To get the `TXT` record value for `DNS` challenge:

```Powershell
certes order authz https://acme-v02.api.letsencrypt.org/acme/order/2/3 *.example.com dns
```

The output will contain the `TXT` record value.

```json
{
  "...": "...",
  "dnsTxt": "Uil-TOCuvR9qnC7H3V65ossmqPgDERDg_9ahr6ZYBd0",
  "resource": "..."
}
```

#### HTTP-01 challenge
 To get token and thumbprint value for `HTTP-01` challenge:

 ```Powershell
certes order authz https://acme-v02.api.letsencrypt.org/acme/order/2/3 api.example.com http
```
The output will contain the two value fields we need to use for order validation.

```json
{
  "location": "...",
  "resource": {
    "type": "http-01",
    "url": "https://acme-v02.api.letsencrypt.org/acme/chall-v3/2645311522/sample",
    "status": "Pending",
    "token": "iuaJR4CdLFxvt4RsmVsgfSU46rqYsrQpzxasdactest"
  },
  "keyAuthz": "iuaJR4CdLFxvt4RsmVsgfSU46rqYsrQpzxasdactest.Qed9-4Ek4ot3idslj89tmCMGEYlfY5I463X37hCd9i4"
}
```

On your application server you need to create file which will be available under *http://api.example.com/.well-known/acme-challenge/iuaJR4CdLFxvt4RsmVsgfSU46rqYsrQpzxasdactest* (resource.token value for last url segment). File content needs to be set as *iuaJR4CdLFxvt4RsmVsgfSU46rqYsrQpzxasdactest.Qed9-4Ek4ot3idslj89tmCMGEYlfY5I463X37hCd9i4* (keyAuthz value).
<!--
TODO: TLS-ALPN-01
-->

### Configure DNS challenge on Azure DNS

If you are using [Azure DNS](https://azure.microsoft.com/en-ca/services/dns) service,
you can setup the `TXT` recod using command:

```PowerShell
certes az dns https://acme-v02.api.letsencrypt.org/acme/order/2/3 `
  --resource-group my-res-grp                                     `
  --subscription-id 00000000-0000-0000-0000-000000000000          `
  --tenant-id 00000000-0000-0000-0000-000000000000                `
  --client-id 00000000-0000-0000-0000-000000000000                `
  --client-secret my-pwd
```

> Azure service principal is used to deploy azure resources. If you don't have
> one already, follow [these steps](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal?view=azure-cli-latest) to create one, and please ensure the 
> application has `DNS Zone Contributor` role assigned.

### Completing Challenges

Once the responses for challenges are ready, we can let the ACME service to
perform validation:

```Powershell
certes order validate https://acme-v02.api.letsencrypt.org/acme/order/2/3 *.example.com dns
certes order validate https://acme-v02.api.letsencrypt.org/acme/order/2/3 api.example.net http
```

The statuses should now changed to `valid` for the authorizations of the domains.

```JSON
{
  "identifier": {
    "type": "dns",
    "value": "*.example.com"
  },
  "status": "valid",
  "expires": "2018-07-24T00:01:32Z",
  "challenges": [
    "..."
  ],
  "wildcard": true
}
```

## Exporting SSL Certificate

Once all the domains are validated, we can finilize the order with a random
private key:

```PowerShell
certes order finalize https://acme-v02.api.letsencrypt.org/acme/order/2/3 `
  --out cert-key.pem
```

> The `--private-key` option can be used to specify the private key for the certificate.

> The `--preferred-chain` option can be used to specify the preferred root certificate. 

To export the certificate in `PEM`:

```PowerShell
certes cert pem https://acme-v02.api.letsencrypt.org/acme/order/2/3 `
  --out my-cert.pem --preferred-chain "ISRG X1 Root"
```

Or pack the certificate and private key in `PFX`:

```PowerShell
certes cert pfx https://acme-v02.api.letsencrypt.org/acme/order/2/3 pfx-password `
  --private-key cert-key.pem                                                     `
  --out my-cert.pfx
```

That's all, you now have your free SSL certificate ready for deploy.

### Deploy SSL Certificate to Azure App Services

Certes CLI also support for deploying the certificates to [Azure App Service](https://azure.microsoft.com/en-us/services/app-service/), `Web App` or `Function App`:

```PowerShell
certes az app https://acme-v02.api.letsencrypt.org/acme/order/2/3 `
  app-svc-name *.example.com                                      `
  --private-key cert-key.pem                                      `
  --resource-group my-res-grp                                     `
  --subscription-id 00000000-0000-0000-0000-000000000000          `
  --tenant-id 00000000-0000-0000-0000-000000000000                `
  --client-id 00000000-0000-0000-0000-000000000000                `
  --client-secret my-pwd
```

> The Azure service principal should have `Website Contributor` role assigned.

> Use the `--slot` option to deploy the SSL certificate to non-production slots.


