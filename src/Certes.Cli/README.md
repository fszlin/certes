# dotnet-certes

`dotnet-certes` is a command-line ACME client built on the
[Certes](https://www.nuget.org/packages/Certes/) library. It manages ACME
accounts, orders and challenges, and exports issued certificates as PEM or PFX.

Requires the .NET 10 runtime.

## New in 4.1

- Select a certificate profile with `order new --profile`, and associate a
  replacement order with an earlier certificate using `--replaces`.
- Query a certificate's suggested renewal window with `cert renewal-info`;
  no account key is required for this query.
- Pass IP addresses to `order new` to create IP identifiers automatically.
- Select `tls-alpn` challenges with `order authz` and `order validate`.
  The CLI outputs the key authorization; the validation certificate must be
  created separately, for example with the library's `TlsAlpnCertificate` helper.

Profile, ARI, and IP certificate support depends on the ACME server. See the
[CLI guide](https://github.com/fszlin/certes/blob/main/docs/CLI.md) for the
complete workflow.

## Getting started

```sh
dotnet tool install --global dotnet-certes
certes --help
```

Settings, including the account key, are stored in a user settings file. Keep
that file private.

## Documentation

- [CLI usage](https://github.com/fszlin/certes/blob/main/docs/CLI.md)
- [Changelog and breaking changes](https://github.com/fszlin/certes/blob/main/docs/CHANGELOG.md)
- [Issues](https://github.com/fszlin/certes/issues)

Licensed under the [MIT license](https://github.com/fszlin/certes/blob/main/LICENSE).
