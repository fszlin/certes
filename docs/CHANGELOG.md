# Changelog
All notable changes to this project will be documented in this file.

## [Unreleased]
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

[Unreleased]: https://github.com/fszlin/certes/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/fszlin/certes/compare/v1.0.7...v1.1.0

[i5]: https://github.com/fszlin/certes/issues/5
