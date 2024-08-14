![Consumer Data Right Logo](./Assets/cdr-logo.png?raw=true) 

[![Consumer Data Standards v1.31.0](https://img.shields.io/badge/Consumer%20Data%20Standards-v1.31.0-blue.svg)](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.31.0/#introduction)
[![Conformance Test Suite Data Holders 5.0.0](https://img.shields.io/badge/Conformance%20Test%20Suite%20Data%20Holders-v5.0.0-darkblue.svg)](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-holders)
[![Conformance Test Suite Data Recipients 4.3.0](https://img.shields.io/badge/Conformance%20Test%20Suite%20Data%20Recipients-v4.3.0-darkblue.svg)](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-recipients)
[![FAPI 1.0 Advanced Profile](https://img.shields.io/badge/FAPI%201.0-orange.svg)](https://openid.net/specs/openid-financial-api-part-2-1_0.html)
[![made-with-dotnet](https://img.shields.io/badge/Made%20with-.NET-1f425Ff.svg)](https://dotnet.microsoft.com/)
[![made-with-csharp](https://img.shields.io/badge/Made%20with-C%23-1f425Ff.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![MIT License](https://img.shields.io/github/license/ConsumerDataRight/mock-data-holder)](./LICENSE)
[![Pull Requests Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](./CONTRIBUTING.md)

# Consumer Data Right - Authorisation Server
This project contains source code, documentation and deployment artefacts for a FAPI 1.0 compliant Authorisation Server, built to conform to the Consumer Data Standards and CDR.

The project is used in the Participant Tooling Authorisation Server, providing the Infosec functionality.  The [repository](https://github.com/ConsumerDataRight) is also provided to the CDR community for use within participant solutions.

## Authorisation Server - Alignment
The Authorisation Server:

-   aligns to  [v1.31.0](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.31.0/#introduction)  of the  [Consumer Data Standards](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.31.0/#introduction)  in particular  [FAPI 1.0 Migration Phase 4](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.31.0/#introduction)  with backwards compatibility to Migration Phase 2 and 3;
-   has passed v5.0.0 of the  [Conformance Test Suite for Data Holders](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-holders); 
-   has passed v4.3.0 of the  [Conformance Test Suite for Data Recipients](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-recipients);
and
-   is certified with the [FAPI 1.0 Advanced Profile](https://openid.net/specs/openid-financial-api-part-2-1_0.html)  .

Note: Consumer Data Standards FAPI 1.0 Migration Phase 1 is no longer supported.

### Financial Grade API (FAPI) 1.0 Advanced Profile Certification
![Authorisation Server - OpenID Certification Mark](http://openid.net/wordpress-content/uploads/2016/04/oid-l-certification-mark-l-rgb-150dpi-90mm-300x157.png)

The Authorisation Server is certified by [OpenID](https://openid.net/connect/) using OpenID FAPI conformance testing. Testing was completed and passed using: 
 - FAPI 1.0 Advanced Test Plan; using
 - CDR Australia profile; and 
 - the latest AU-CDR Adv. OP w/ Private Key, PAR, JARM iteration

Full test results for the Authorisation Server can be seen on the [OpenID website](https://www.certification.openid.net/plan-detail.html?plan=BGnDgdFgYro9d&public=true).
Certification for the Authorisation Server can be seen under the Australia CDR profile section on the [OpenID website](https://openid.net/certification/#FAPI_OPs)

![Authorisation Server - FAPI Certification](./Assets/authorisation-server-fapi-certification.png?raw=true)

## Getting Started
The Authorisation Server can be used for providing authentication to the Banking and Energy [Mock Data Holders](https://github.com/ConsumerDataRight/mock-data-holder). You can swap out any of the Mock Data Holders and [Mock Data Recipient](https://github.com/ConsumerDataRight/mock-data-recipient) solutions with a solution of your own.

Please note that the Authorisation Server can also run as an embedded component of the Banking and Energy [Mock Data Holders](https://github.com/ConsumerDataRight/mock-data-holder) solution. This is not covered in this guide.

There are a number of ways that the artefacts within this project can be used:
1. Build and deploy the source code
2. Use the pre-built image

### Build and deploy the source code

To get started, clone the source code.
```
git clone https://github.com/ConsumerDataRight/authorisation-server.git
```

To get help on launching and debugging the solution, see the [help guide](./Help/debugging/HELP.md). 

If you would like to contribute features or fixes back to the Authorisation Server repository, consult the [contributing guidelines](./CONTRIBUTING.md).

### Use the pre-built image

A version of the Authorisation Server is built into a single Docker image that is made available via [docker hub](https://hub.docker.com/r/consumerdataright/authorisation-server). 

#### Pull the latest image

```
docker pull consumerdataright/authorisation-server
```

To get help on launching the solution in a container, see the [help guide](./Help/container/HELP.md).

#### Try it out

The OpenID Connect Discovery document for Authorisation Server can be viewed by accessing:
```
https://localhost:8001/.well-known/openid-configuration
```
Since the Authorisation Server is an embedded component of the [Banking and Energy Mock Data Holder](https://github.com/ConsumerDataRight/mock-data-holder) solutions, the Authorisation Server's functionality can be further explored by using the Mock CDR Ecosystem.
The [Mock Data Recipient](https://github.com/ConsumerDataRight/mock-data-recipient) has been built as a test harness to demonstrate the interactions between the Register and Mock Data Holders with an embedded Authorisation Server.
The Mock Data Recipient can be used to perform a Dynamic Client Registration, create Consent Arrangements and explore Mock Data Holder API's.
The provided [help guide](https://github.com/ConsumerDataRight/mock-data-recipient/blob/main/Help/container/HELP.md) will assist in setting up the Mock CDR Ecosystem in Docker.

#### Certificate Management

Consult the [Certificate Management](./CertificateManagement/README.md) documentation for more information about how certificates are used for the Authorisation Server.

#### Loading your own data

The Authorisation Server contains seed data files in a json format. The Authorisation Server will read directly from these files when:
 - Loading Customer and Account data for use in the Consent and Authorisation flow [User Interface](./Source/CdrAuthServer.UI/src/models/DataModels.ts).
 - Adding user name claims during a [token request](./Source/CdrAuthServer/Services/TokenService.cs).

The following steps required to load your own data into the container instance or code running in Visual Studio:
1. Within the `/Source/CdrAuthServer/Data` folder of the container or source code directory, make a copy of the `customer-seed-data.json` file for banking customers or the `customer-seed-data-energy.json` file for energy customers, renaming to a name of your choice, e.g. `my-new-customer-seed-data.json`.
2. Update your seed data file with your desired metadata.
3. Change the `/Source/CdrAuthServer/appsettings.json` file to load the new data file for use in the APIs: 

```
"SeedData": {
    "FilePath": "Data/my-new-customer-seed-data.json",
},
```

4. Copy the updated seed data file to 'Source/CdrAuthServer.UI/public'
5. Change the `/Source/CdrAuthServer.UI/.env.*` file to load the new data file for use in the User Interface:

```
REACT_APP_DATA_FILE_NAME=my-new-customer-seed-data.json
```

6. Restart the container or restart the application in Visual Studio.

To get help on launching the solution in a container, see the [help guide](./Help/container/HELP.md).

## Authorisation Server - Architecture
The following diagram outlines the high level architecture of the Authorisation Server
![Authorisation Server - Architecture](./Assets/authorisation-server-architecture.png?raw=true)

The following diagram illustrates the docker container for the Authorisation Server
![Authorisation Server - Docker Container](./Assets/authorisation-server-docker-container.png?raw=true)

The following diagram illustrates the high level features for the Authorisation Server
![Authorisation Server - Features](./Assets/authorisation-server-visual-studio.png?raw=true)

## Authorisation Server - Components
The Authorisation Server contains the following components:

- Authorisation Server
  - Hosted at `https://localhost:8001`
  - CDR Authorisation Server implementation utilising (this) `cdr-auth-server`
  - Accessed directly (TLS only) as well as the mTLS Gateway, depending on the target endpoint.
- mTLS Gateway
  - Hosted at `https://localhost:8002`
  - Provides the base URL endpoint for mTLS communications, including Infosec, Resource and Admin APIs.
  - Performs certificate validation.
- Resource API
  - Hosted at `https://localhost:8001`
  - Currently includes the `Get Customer` endpoint.
  - Accessed via the mTLS Gateway.
- UI
  - Hosted at `https://localhost:3000`
- Azure Function
  - An Azure Function that can automate the continuous Get Data Recipients discovery process.
  - To get help on the Azure Functions, see the [help guide](./Help/azurefunctions/HELP.md).
- Repository
  - A SQL database containing clients, grants and log entries for identity provider solutions.

## Technology Stack
The following technologies have been used to build the Authorisation Server:
- The source code has been written in `C#` using the `.Net 8` framework.
- The mTLS Gateway has been implemented using `Ocelot`.
- The Repository utilises a `SQL` instance.
- `xUnit` is the framework used for writing and running tests.
- `Microsoft Playwright` is the framework used for Web Testing.

# Features
The Authorisation Server contains the following features:
- OpenID Discovery Document
- JWKS endpoint
- Dynamic Client Registration (DCR)
- Pushed Authorization Requests
- Authorization endpoint
    - Support for request_uri parameter
    - Hybrid and Authorization Code flow
    - Conforms to Consumer Data Right [CX Guidelines](https://consumerdatastandards.gov.au/guidelines-and-conventions/consumer-experience-guidelines).
- Token endpoint
    - Authorization Code
    - Client Credentials
    - Refresh Token
- UserInfo
- Introspection
- Token Revocation
- CDR Arrangement Revocation

# Endpoints
The Authorisation Server has the following endpoints:

| Endpoint                          | Methods          | Transport | Authorisation    | Description                |
|-----------------------------------|------------------|-----------|------------------|----------------------------|
| /.well-known/openid-configuration | GET              | TLS       | None             | Open ID Discovery Document |
| /jwks                             | GET              | TLS       | None             | JWKS endpoint |
| /auth                             | GET              | TLS       | None             | Authorization endpoint |
| /registration                     | POST             | mTLS      | None             | Create registration endpoint (DCR) |
| /registration                     | GET, PUT, DELETE | mTLS      | Bearer           | Create registration endpoint (DCR) |
| /par                              | POST             | mTLS      | Client Assertion | Pushed Authorization Request endpoint (PAR) |
| /token                            | POST             | mTLS      | Client Assertion | Token endpoint |
| /userinfo                         | POST             | mTLS      | Bearer           | Userinfo endpoint |
| /introspect                       | POST             | mTLS      | Client Assertion | Introspection endpoint |
| /token/revocation                 | POST             | mTLS      | Client Assertion | Token revocation endpoint |
| /arrangement/revocation           | POST             | mTLS      | Client Assertion | Arrangement revocation endpoint |

# Customisations
Standard, "out-of-the-box" Authorisation Server functionality needs to be customised in order to meet the Consumer Data Standards.

The information below lists the customisation required for each endpoint:

##  /.well-known/openid-configuration
- No customisation required.

## /jwks
- No customisation required.

## /auth
- The authorisation flow defined for CDS is customised and includes OTP and account selection steps.

## /registration
- The DCR process relies on an SSA issued by the Register.  
- The SSA signature needs to be verified against the Register SSA JWKS.
- The contents of the registration request needs to be validated against the contents of the SSA.
- The contents of the registration request needs to be validated against the CDS.

## /par
- Validation of the request object must conform to CDS.
- Need to extract and interpret the custom claims (`cdr_arrangement_id`, `sharing_duration`) from the request object.

## /token
- Must create a CDR arrangement, based on the sharing_duration claim.
- Must return the `cdr_arrangement_id` claim in the payload.

## /userinfo
- The `sub` claim needs to be obfuscated using the PPID rules of the CDS.

## /introspect
- This endpoint can only be used for refresh tokens, not access tokens.
- The payload needs to be include the `cdr_arrangement_id` claim.

## /token/revocation
- No customisation required.

## /arrangement/revocation
- This is a custom endpoint for CDS.

# Testing
Automated tests have been created as part of this solution. See the [Test Automation Execution Guide](./Help/testing/HELP.md) documentation for more information.

# Contribute
We encourage contributions from the community.  See our [contributing guidelines](./CONTRIBUTING.md).

# Code of Conduct
This project has adopted the **Contributor Covenant**.  For more information see the [code of conduct](./CODE_OF_CONDUCT.md).

# Security Policy
See our [security policy](./SECURITY.md) for information on security controls, reporting a vulnerability and supported versions.

# License
[MIT License](./LICENSE)

# Notes
The Authorisation Server is provided as a development tool only.  It conforms to the Consumer Data Standards. 