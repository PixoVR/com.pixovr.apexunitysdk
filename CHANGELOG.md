# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2022-05-31
### This is the first release of *com.pixovr.apexunitysdk*.

## [1.0.1] - 2022-06-03
### Updates the Newtonsoft.Json dependency to the latest version and rebuilt TinCan.Net along side it.
### Make changes to the Sample to fix UI orientation.
### Added new changes to the README to reflect info for building with various versions of Unity.

## [1.0.2] - 2022-06-04
### Added **SendSessionEvent** to allow sending events during a session.
### Updated documentation for **SendSessionEvent**.
### Added static properties to ApexSystem.

## [1.0.3] - 2022-06-07
### Created Extension which simplifies adding extension data for xAPI.
### Added Context Extension parameter to **JoinSession**.
### Added Result and Context Extension parameters to **CompleteSession**.
### Added the missing Statement to **SendSessionEvent**.
### Changed the platform variable to reflect the actual platform.
### Added deviceId and deviceModel to any event within the statement context.

## [1.0.4] - 2022-06-08
### Exception catching for Extension.
### Fixed incorrect IRIs within **JoinSession**, **SendEvent** and **CompleteSession**.
### Fixed incorrect IRIs within the Sample provided.
### Added documentation to point to xAPI spec and TinCan.NET documentation.

## [1.0.5] - 2022-07-19
### Removed custom event name being passed into the SendSessionEvent function.
### Added support to create an Extension from a jobject.
### Merge any existing extensions from the SendSessionEvent context.