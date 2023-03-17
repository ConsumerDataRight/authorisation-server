@echo off
echo Run solutions from .Net CLI using localhost and localdb from appsettings.Development.json

setx ASPNETCORE_ENVIRONMENT Development

dotnet build CdrAuthServer.mTLS.Gateway
dotnet build CdrAuthServer.TLS.Gateway
dotnet build CdrAuthServer

wt --maximized ^
--title AUTH_SERVER_MTLS -d CdrAuthServer.mTLS.Gateway dotnet run --launch-profile CdrAuthServer.mTLS.Gateway; ^
--title AUTH_SERVER_TLS -d CdrAuthServer.TLS.Gateway dotnet run --launch-profile CdrAuthServer.TLS.Gateway; ^
--title AUTH_SERVER -d CdrAuthServer dotnet run --launch-profile CdrAuthServer