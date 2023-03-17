#Requires -PSEdition Core

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Output "***********************************************************"
Write-Output "CdrAuthServer integration tests (Standalone)"
Write-Output ""
Write-Output "⚠ WARNING: Integration tests for CdrAuthServer will use the existing 'mock-register' image found on this machine. Rebuild that image if you wish to test with latest code changes for MockRegister"
Write-Output "***********************************************************"

# Run integration tests
docker-compose -f docker-compose.IntegrationTests.Standalone.yml up --build --abort-on-container-exit --exit-code-from cdr-auth-server-integration-tests
$_lastExitCode = $LASTEXITCODE

# Stop containers
docker-compose -f docker-compose.IntegrationTests.Standalone.yml down

if ($_lastExitCode -eq 0) {
    Write-Output "***********************************************************"
    Write-Output "✔ SUCCESS: CdrAuthServer integration tests (Standalone) passed"
    Write-Output "***********************************************************"
    exit 0
}
else {
    Write-Output "***********************************************************"
    Write-Output "❌ FAILURE: CdrAuthServer integration tests (Standalone) failed"
    Write-Output "***********************************************************"
    exit 1
}
