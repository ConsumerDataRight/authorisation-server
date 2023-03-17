#Requires -PSEdition Core

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Output "***********************************************************"
Write-Output "CDR-Auth-Server unit tests"
Write-Output "***********************************************************"

# Build and run containers
 docker-compose -f docker-compose.UnitTests.yml up --build --abort-on-container-exit --exit-code-from cdr-auth-server-unit-tests
$_lastExitCode = $LASTEXITCODE

# Stop containers
docker-compose -f docker-compose.UnitTests.yml down

if ($_lastExitCode -eq 0) {
    Write-Output "***********************************************************"
    Write-Output "✔ SUCCESS: CDR-Auth-Server unit tests passed"
    Write-Output "***********************************************************"
    exit 0
}
else {
    Write-Output "***********************************************************"
    Write-Output "❌ FAILURE: CDR-Auth-Server unit tests failed"
    Write-Output "***********************************************************"
    exit 1
}
