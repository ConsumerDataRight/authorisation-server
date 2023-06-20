openssl req -new -newkey rsa:2048 -keyout mtls-server.key -sha256 -nodes -out mtls-server.csr -config mtls-server.cnf
openssl req -in mtls-server.csr -noout -text
openssl x509 -req -days 1826 -in mtls-server.csr -CA ca.pem -CAkey ca.key -CAcreateserial -out mtls-server.pem -extfile mtls-server.ext
openssl pkcs12 -inkey mtls-server.key -in mtls-server.pem -export -out mtls-server.pfx
openssl pkcs12 -in mtls-server.pfx -noout -info