#Requires -PSEdition Core

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Output "***********************************************************"
Write-Output "CdrAuthServer E2E tests (Standalone)"
Write-Output ""
Write-Output "⚠ WARNING: E2E tests for CdrAuthServer E2E will use the existing 'mock-register' image found on this machine. Rebuild that image if you wish to test with latest code changes for MockRegister"
Write-Output "***********************************************************"

# Run E2E tests
docker-compose -f docker-compose.E2ETests.Standalone.yml up --build --abort-on-container-exit --exit-code-from cdr-auth-server-e2e-tests
$_lastExitCode = $LASTEXITCODE

# Stop containers
docker-compose -f docker-compose.E2ETests.yml down

if ($_lastExitCode -eq 0) {
    Write-Output "***********************************************************"
    Write-Output "✔ SUCCESS: CdrAuthServer E2E tests (Standalone) passed"
    Write-Output "***********************************************************"
    exit 0
}
else {
    Write-Output "***********************************************************"
    Write-Output "❌ FAILURE: CdrAuthServer E2E tests (Standalone) failed"
    Write-Output "***********************************************************"
    exit 1
}
