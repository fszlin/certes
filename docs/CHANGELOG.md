# Changelog
All notable changes to this project will be documented in this file.

## [2.0.0] - 2018-03-13
### Added
- [ACME v2](APIv2.md) support
- Add support for JSON web signature using ECDSA key

## [1.1.4]
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

[Unreleased]: https://github.com/fszlin/certes/compare/v2.0.0...HEAD
[1.1.0]: https://github.com/fszlin/certes/compare/v1.0.7...v1.1.0
[1.1.1]: https://github.com/fszlin/certes/compare/v1.1.0...v1.1.1
[1.1.2]: https://github.com/fszlin/certes/compare/v1.1.1...v1.1.2
[1.1.3]: https://github.com/fszlin/certes/compare/v1.1.2...v1.1.3
[1.1.4]: https://github.com/fszlin/certes/compare/v1.1.3...v1.1.4
[2.0.0]: https://github.com/fszlin/certes/compare/v1.1.3...v2.0.0

[i5]: https://github.com/fszlin/certes/issues/5
[i22]: https://github.com/fszlin/certes/issues/22
