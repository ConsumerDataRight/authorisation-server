# Generates a self signed certificate for the authserver-ui domain name
# Use a password of "#AuthServerU1#"

openssl genrsa -out authserver-ui.key 4096
openssl req -new -key authserver-ui.key -out authserver-ui.csr -config authserver-ui.cnf
openssl x509 -req -days 3650 -in authserver-ui.csr -signkey authserver-ui.key -out authserver-ui.crt -extfile authserver-ui.ext
openssl pkcs12 -inkey authserver-ui.key -in authserver-ui.crt -export -out authserver-ui.pfx

#openssl req -x509 -newkey rsa: -keyout .key -out authserver-ui.crt -sha256 -days 365 -config authserver-ui.cnf -extfile authserver-ui.ext
