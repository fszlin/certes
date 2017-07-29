# Changelog
All notable changes to this project will be documented in this file.

## [Unreleased]
### Added
- Add [Directory][cs-directory] to replace [AcmeDirectory][cs-acme-directory]
- Add support for deleting registration

## 1.0.7 - 2017-07-20
### Changed
- Fix error when parsing directory resource with *meta* property. ([#5][i5])

[Unreleased]: https://github.com/fszlin/certes/compare/v1.0.7...HEAD

[i5]: https://github.com/fszlin/certes/issues/5
[cs-directory]: blob/master/src/Certes/Acme/Resource/Directory.cs
[cs-acme-directory]: blob/master/src/Certes/Acme/AcmeDirectory.cs
