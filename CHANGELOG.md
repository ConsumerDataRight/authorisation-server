# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [3.0.0] - 2025-03-19
 
### Changed
- Updated NuGet packages
- Fixed multiple build warnings to improve code quality and maintainability
- Updated npm packages used for Authorisation Server User Interface.

### Removed
- Removed all OIDC Hybrid Flow related code and functionality

## [2.1.0] - 2024-08-16
 
### Changed
- Updated NuGet packages

## [2.0.0] - 2024-06-12
 
### Changed
- Migrated from .Net 6 to .Net 8
- Migrated docker compose from v1 to v2

### Added
- Added SSL Server Validation configuration capability 

### Removed
- Postman files have been removed

## [1.2.0] - 2024-03-13
 
### Changed
- Updated Nuget package references.
- Updated automated tests to use ACF as default instead of Hybrid Flow.
- Added cleanup of arrangement data in Authorisation Server for Data Holder to Data Recipient initiated revocation 

### Fixed
- Updated error handling when encryption key missing for a client

 
## [1.1.2] - 2024-02-14
 
### Changed
- Updated the message body for arrangement revocation request to MDR to include request and response details
- Replaced Recoil with React Hooks useContext to provide state management within the UI
- Updated node image version to 20 for docker build 
 
## [1.1.1] - 2023-11-29

### Changed
- Updated npm packages used for Authorisation Server User Interface.

## [1.1.0] - 2023-10-26

### Changed
- Updated npm packages used for Authorisation Server User Interface.
- Refactored automated tests to use a shared NuGet package.

## [1.0.3] - 2023-10-03

### Changed
- Configurable OSCP check added for mtls client certificates. By default it is disabled.

## [1.0.2] - 2023-06-20

### Changed
- Regenerated all mTLS, SSA and TLS certificates to allow for another five years before they expire.

## [1.0.1] - 2023-06-07

### Changed
- GitHub actions to publish test report and use v2 version of CodeQL commands.

### Fixed 
- Added Client Id to access token response when no Client Id was provided in the request

## [1.0.0] - 2023-02-16

### Added
- First release of Authorisation Server.
