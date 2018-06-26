
# Generate certificates using Certes CLI

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

To use an existing account, the account key can be imported as:

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
}
```

## Ordering SSL Certificates

With an `valid` ACME account, we can start generating SSL certificates now.

> You may add up to 100 domains in one order, and mixing wildcard and non-wildcard
> domains, as long as the domains don't overlap with each other.

```PowerShell
certes order new *.example.com api.example.net
```

Keep a note of the order location, which we will use it in the next steps:

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

Certes CLI provides commands for generating necessary data to fullfill
the challenges. To get the `TXT` record value for `DNS` challenge:

```Powershell
certes order authz https://acme-v02.api.letsencrypt.org/acme/order/2/3 *.example.com dns
```

The output will contain the `TXT` record value.

```json
{
  // ...
  "dnsTxt": "Uil-TOCuvR9qnC7H3V65ossmqPgDERDg_9ahr6ZYBd0",
  "resource": {
      // ...
  }
}
```
