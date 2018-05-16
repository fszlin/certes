# Changelog
All notable changes to this project will be documented in this file.

## [Unreleased]

## [2.2.1] - 2018-05-15
### Added
- Strong name signing ([#106][i106])

## [2.2.0] - 2018-05-05
### Added
- Add `Challenge.Error` for challenge valdidation error ([#99][i99])

### Changed
- Fix certificate chain for PFX ([#100][i100])

## [2.1.0] - 2018-04-27
### Added
- Export full chain certification in PEM ([#87][i87])
- Support custom `HttpClient` ([#95][i95])

### Changed
- Encapsulating ACME errors in exceptions ([#65][i65])
- [CLI] Change argument ~~talent-id~~ to `tenant-id` ([#86][i86])

## [2.0.1] - 2018-03-17
### Added
- Add `Processing` status for challenges.

### Changed
- Fix `Content-Type` header for POST requests ([#76][i76])

## [2.0.0] - 2018-03-13
### Added
- [ACME v2](APIv2.md) support
- Add support for JSON web signature using ECDSA key

## [1.1.4] - 2018-03-04
### Changed
- Fix error when processing server response without `Content-Type` header
- Fix `full-chain-off` option for CLI

## [1.1.3] - 2017-11-23
### Changed
- Fix MissingFieldException when running with BouncyCastle v1.8.1.3 ([#22][i22])

## [1.1.2] - 2017-09-27
### Changed
- Fix CLI script error

## [1.1.1] - 2017-09-26
### Added
- Add build target for net47

## [1.1.0] - 2017-08-17
### Added
- Add `Directory` to replace `AcmeDirectory`
- Add support for deleting registration
- Add support for account key roll-over

### Changed
- Allow assigning SAN when creating `CertificationRequestBuilder` instances

## 1.0.7 - 2017-07-20
### Changed
- Fix error when parsing directory resource with *meta* property. ([#5][i5])

[1.1.0]: https://github.com/fszlin/certes/compare/v1.0.7...v1.1.0
[1.1.1]: https://github.com/fszlin/certes/compare/v1.1.0...v1.1.1
[1.1.2]: https://github.com/fszlin/certes/compare/v1.1.1...v1.1.2
[1.1.3]: https://github.com/fszlin/certes/compare/v1.1.2...v1.1.3
[1.1.4]: https://github.com/fszlin/certes/compare/v1.1.3...v1.1.4
[2.0.0]: https://github.com/fszlin/certes/compare/v1.1.4...v2.0.0
[2.0.1]: https://github.com/fszlin/certes/compare/v2.0.0...v2.0.1
[2.1.0]: https://github.com/fszlin/certes/compare/v2.0.1...v2.1.0
[2.2.0]: https://github.com/fszlin/certes/compare/v2.1.0...v2.2.0
[2.2.1]: https://github.com/fszlin/certes/compare/v2.2.0...v2.2.1
[Unreleased]: https://github.com/fszlin/certes/compare/v2.2.1...HEAD

[i5]: https://github.com/fszlin/certes/issues/5
[i22]: https://github.com/fszlin/certes/issues/22
[i65]: https://github.com/fszlin/certes/issues/65
[i76]: https://github.com/fszlin/certes/issues/76
[i86]: https://github.com/fszlin/certes/issues/86
[i87]: https://github.com/fszlin/certes/issues/87
[i95]: https://github.com/fszlin/certes/issues/95
[i99]: https://github.com/fszlin/certes/issues/99
[i100]: https://github.com/fszlin/certes/issues/100
[i106]: https://github.com/fszlin/certes/issues/106
