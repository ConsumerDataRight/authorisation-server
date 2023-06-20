docker build -t cdr-auth-server:fapi-testing -f ../Source/Dockerfile ../Source/.

az acr login --name acrcdrsandboxdev --resource-group rg-applications-dev --username acrcdrsandboxdev --password [Get password for ACR]
docker tag cdr-auth-server:fapi-testing acrcdrsandboxdev.azurecr.io/cdr-auth-server:fapi-testing
docker push acrcdrsandboxdev.azurecr.io/cdr-auth-server:fapi-testing

#az container delete --name aci-cdr-auth-server-dev --resource-group rg-cdr-auth-server-dev --yes
#az container create -g rg-cdr-auth-server-dev --name aci-cdr-auth-server-dev --image acrcdrsandboxdev.azurecr.io/cdr-auth-server:fapi-testing --registry-login-server acrcdrsandboxdev.azurecr.io --registry-username acrcdrsandboxdev --registry-password [Get password for ACR] --ports 5001 5002 5003 --dns-name-label cdr-auth-server-dev --environment-variables CdrAuthServer__BaseUri=https://cdr-auth-server-dev.australiaeast.azurecontainer.io:5001 CdrAuthServer__SecureBaseUri=https://cdr-auth-server-dev.australiaeast.azurecontainer.io:5002 CdrAuthServer__Issuer=https://cdr-auth-server-dev.australiaeast.azurecontainer.io:5001 CdrAuthServer__CdrRegister__SsaJwksUri=https://api.dev.cdrsandbox.gov.au/cdr-register/v1/jwks Certificates__RootCACertificate__Location=Certificates/root-ca-dev.crt Certificates__MtlsServerCertificate__Location=Certificates/mtls-server-sandbox-dev.pfx Certificates__MtlsServerCertificate__Password=df00eaf59e014881a9c3b27dd5482c31 AccessTokenIntrospectionEndpoint=https://cdr-auth-server-dev.australiaeast.azurecontainer.io:5001/connect/introspect-internal

az container restart --name aci-cdr-auth-server-dev --resource-group rg-cdr-auth-server-dev 
