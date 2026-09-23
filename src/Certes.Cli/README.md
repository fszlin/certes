# dotnet-certes

`dotnet-certes` is a command-line ACME client built on the
[Certes](https://www.nuget.org/packages/Certes/) library. It manages ACME
accounts, orders and challenges, and exports issued certificates as PEM or PFX.

Requires the .NET 10 runtime.

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
