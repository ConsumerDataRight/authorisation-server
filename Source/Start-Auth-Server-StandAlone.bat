@echo off
echo Run solutions from .Net CLI using localhost and localdb from appsettings.Development.json

setx ASPNETCORE_ENVIRONMENT Development

dotnet build CdrAuthServer

wt --maximized ^
--title AUTH_SERVER -d CdrAuthServer dotnet run --launch-profile MDH-CdrAuthServer