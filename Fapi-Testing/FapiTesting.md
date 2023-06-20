# FAPI Conformance Testing

FAPI Conformance Testing was performed using OIDF's [conformance suite](https://www.certification.openid.net/index.html).

## FAPI 1.0 Phase 2 (Hybrid) Testing

### Configuration
- The CDR Auth Server container was hosted in an *Azure Container Instance* at https://cdr-auth-server-dev.australiaeast.azurecontainer.io:5001
- Ports 5001 (TLS Gateway) and 5002 (mTLS Gateway) were exposed.
- The CDR Sandbox DEV Certificate Authority was used and trusted.

### Test Setup
- Name: fapi1-advanced-final-test-plan
- Variant: client_auth_type=private_key_jwt, fapi_auth_request_method=pushed, fapi_profile=consumerdataright_au, fapi_response_mode=plain_response
- The FAPI Conformance Suite was configured with 2 clients (see configuration below).

### Results
- Pass

![module results](fapi-hybrid-results.png)

- 3 known errors:
	- Cipher Suite support - this will not be an issue when hosted behind the App Gateway, as per current Sandbox architecture.
	- mTLS client certificate error - the container doesn't return an error, just drops the connection.  Like above, will not be an issue once hosted behind the App Gateway.
	- User cancel option - there is no UI yet, so the end user cannot cancel the flow.

## FAPI 1.0 Phase 3 (Auth Code Flow) Testing
Phase 3 introduces the authorization code flow and JARM.

**Disclaimer:** the FAPI conformance suite does not support JARM testing with the CDR profile.  Therefore, testing occurred using the plain FAPI profile which has a number of differences:
- The PAR endpoint is not mTLS.
- A different ACR is used in the id_token.

### Test Setup
- Name: fapi1-advanced-final-test-plan
- Variant: client_auth_type=private_key_jwt, fapi_auth_request_method=pushed, fapi_profile=plain_fapi, fapi_response_mode=jarm
- The FAPI Conformance Suite was configured with 2 clients (see configuration below).

### Results
- Pass

![module results](fapi-auth-code-results.png)

- Known errors, as per hybrid testing.

## FAPI Client Configuration

```
{
    "alias": "cdr-auth-server",
    "description": "CDR Auth Server",
    "server": {
        "discoveryUrl": "https://cdr-auth-server-dev.australiaeast.azurecontainer.io:5001/.well-known/openid-configuration"
    },
    "client": {
        "client_id": "3e6c5f3d-bd58-4aaa-8c23-acfec837b506",
        "scope": "openid profile common:customer.basic:read energy:accounts.basic:read energy:accounts.concessions:read cdr:registration",
        "jwks": {
            "keys": [
                {
                    "alg": "PS256",
                    "e": "AQAB",
                    "use": "sig",
                    "kid": "7EFA85C18FDE857949BC2EAA21C25E49627D4865",
                    "kty": "RSA",
                    "n": "wsfaGZ_bVTgF8tl8oy79iVmLZ_nCdAiz5cptwP69qRQB-K0J6DNiAB3DOZ-rlgPZHtWwVGTNvvVE48uY6zWvghj2M7FpxjjqVHCoL98mh5ZLAToucQT3MfVJ62laS4iy6nLnAh9RdUqIrdT4Pji8BajFva1TWWBRYpHPykDuv2ZClRglScrmuIlVOXwDWNCXvIGE4qGwEX2m3UGYFPEv3dkBinJxcsbmEfNcWnjneoyWemcw9x55d-3DxwpLKfjUEu0uxChYTDVk9Ii3uzZls9qHVBwOtrT6c9AKJmtC_slqMtaqxvgBETI8tH8Jl3-5jNoQg094xLdzXftHc06dsw",
                    "d": "Cx5XX9EVNxccl9E8YSBEjruSzpueMvtwMXTNsQ-ZifY_ao-OGjgcpv8L7tUjeUu88Bqolxit-fGMPiiYEQ0eeKGuJCNDc3I6RhmsMBdf3quAmpBUqFTtO2fSEWMRKXCjLejjMObSwow_oxSeGwcoDHam2v3y3Q43dxX1s4jjV_-HuwgwghOqd9gTWeu7tOQefYkJ4Tsj6UMrf42LbSazjQmz-4sABmYv1TiuGW9Uj-vBiV7Jozc9rVx4ZOhKrGxnM8kMsG6RWCfP5Nm3PUM_9tCzqNcPJCN3FyEcj2tfU_0ICBuUqXaX4V_usg-TOf2tO2n_5TIZQozUatOAhWeJcQ"
                },
                {
                    "alg": "RSA-OAEP",
                    "e": "AQAB",
                    "use": "enc",
                    "kid": "e2c6eaff7fad081c78b22d63bbb567e86070e51eb1476abca6a233bce8ca14ba",
                    "kty": "RSA",
                    "n": "wsfaGZ_bVTgF8tl8oy79iVmLZ_nCdAiz5cptwP69qRQB-K0J6DNiAB3DOZ-rlgPZHtWwVGTNvvVE48uY6zWvghj2M7FpxjjqVHCoL98mh5ZLAToucQT3MfVJ62laS4iy6nLnAh9RdUqIrdT4Pji8BajFva1TWWBRYpHPykDuv2ZClRglScrmuIlVOXwDWNCXvIGE4qGwEX2m3UGYFPEv3dkBinJxcsbmEfNcWnjneoyWemcw9x55d-3DxwpLKfjUEu0uxChYTDVk9Ii3uzZls9qHVBwOtrT6c9AKJmtC_slqMtaqxvgBETI8tH8Jl3-5jNoQg094xLdzXftHc06dsw",
                    "d": "Cx5XX9EVNxccl9E8YSBEjruSzpueMvtwMXTNsQ-ZifY_ao-OGjgcpv8L7tUjeUu88Bqolxit-fGMPiiYEQ0eeKGuJCNDc3I6RhmsMBdf3quAmpBUqFTtO2fSEWMRKXCjLejjMObSwow_oxSeGwcoDHam2v3y3Q43dxX1s4jjV_-HuwgwghOqd9gTWeu7tOQefYkJ4Tsj6UMrf42LbSazjQmz-4sABmYv1TiuGW9Uj-vBiV7Jozc9rVx4ZOhKrGxnM8kMsG6RWCfP5Nm3PUM_9tCzqNcPJCN3FyEcj2tfU_0ICBuUqXaX4V_usg-TOf2tO2n_5TIZQozUatOAhWeJcQ"
                }
            ]
        }
    },
    "mtls": {
        "cert": "-----BEGIN CERTIFICATE-----\nMIIEJTCCAt2gAwIBAgIIecSLYQAAAAAwPQYJKoZIhvcNAQEKMDCgDTALBglghkgB\nZQMEAgGhGjAYBgkqhkiG9w0BAQgwCwYJYIZIAWUDBAIBogMCASAwajELMAkGA1UE\nBhMCQVUxDTALBgNVBAoMBEFDQ0MxDDAKBgNVBAsMA0NEUjEYMBYGA1UECwwPQ0RS\nIFNhbmRib3ggZGV2MSQwIgYDVQQDDBtDRFIgU2FuZGJveCBJbnRlcm1lZGlhdGUg\nQ0EwHhcNMjExMTA5MTMwOTEyWhcNMjIxMTEwMTMwOTEyWjBkMQswCQYDVQQGEwJB\nVTEcMBoGA1UECxMTQ29uc3VtZXIgRGF0YSBSaWdodDETMBEGA1UEChMKTURSIChB\nQ0NDKTEiMCAGA1UEAxMZTW9jayBEYXRhIFJlY2lwaWVudCAoREVWKTCCASIwDQYJ\nKoZIhvcNAQEBBQADggEPADCCAQoCggEBAMyx16fcRhPczhdr40YEMa7Eh/l2QSI8\na1uDuArKASzWmJT92fVNJqrAl4HyFH2gg5WxLtT8b8ltzGzO8oOTSIr646Z0RxCR\n9/ilhmtUeWGzdmfUENy5S+WVJf/cIa3Ai/EnLKNnyapwk9cJBjrJpjO+UjmIc/Gy\nw7grqmQMnMYTOshS6xWIn0Ia9AGnpHzxHKwduSOs8gGxKDa9hUuMUmQTNuwWAq7R\nKrSEMaT9dTfrjPkIuTX6wFtB60CMi4q41duNQbHoBrgksO61HYh0KHFKUcM97d7e\nFjdRHgmCoDPZxFewa4qabk7PJjjMquXcX4QPfIMuNj7kx9TH//qu2DkCAwEAAaN1\nMHMwDAYDVR0TAQH/BAIwADAOBgNVHQ8BAf8EBAMCBaAwHwYDVR0jBBgwFoAU4KJg\nrpAfQQqywrXq4djKu921yyAwEwYDVR0lBAwwCgYIKwYBBQUHAwIwHQYDVR0OBBYE\nFIj6/TlydT4KrZslusA9US7LkxW6MD0GCSqGSIb3DQEBCjAwoA0wCwYJYIZIAWUD\nBAIBoRowGAYJKoZIhvcNAQEIMAsGCWCGSAFlAwQCAaIDAgEgA4IBAQC/ckb/+z5T\nPC1kV5eWUjLOvL1cR7bUu+y+IHLDobNz4Ze2rYjDorJkyZ79Et7RZKK6LvLRrOd/\nKmCoyTxYms5+9Z+A0dafpsdS6DT735bDyv/hb7P62IgZ66yfEPnhpZ+yteJA5NBG\n40igsH1YGSmmy0A8dCpSuAVREKXZi5SaN6ocEGU78v34IIg3s2ZzDzN/OTYjpabQ\nexG6za6sHnG00iNpnHQFGsjSkEg+5tGNopbNwCqv0xj3y1mhNE2tcdMG1fj+AzrA\n1uEw9SDscWyQsEBVeuVcc3TG7xkkbvdy4DjJhOrFtoNLz5LqvCYvLzuXHFHBYQGx\nNNH/Tb50hFh0\n-----END CERTIFICATE-----",
        "key": "-----BEGIN PRIVATE KEY-----\nMIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQDMsden3EYT3M4X\na+NGBDGuxIf5dkEiPGtbg7gKygEs1piU/dn1TSaqwJeB8hR9oIOVsS7U/G/Jbcxs\nzvKDk0iK+uOmdEcQkff4pYZrVHlhs3Zn1BDcuUvllSX/3CGtwIvxJyyjZ8mqcJPX\nCQY6yaYzvlI5iHPxssO4K6pkDJzGEzrIUusViJ9CGvQBp6R88RysHbkjrPIBsSg2\nvYVLjFJkEzbsFgKu0Sq0hDGk/XU364z5CLk1+sBbQetAjIuKuNXbjUGx6Aa4JLDu\ntR2IdChxSlHDPe3e3hY3UR4JgqAz2cRXsGuKmm5OzyY4zKrl3F+ED3yDLjY+5MfU\nx//6rtg5AgMBAAECggEAb8ALz12neIKMlSbWbkwt3471+RBBYPKBXfXcTS+ZUqRZ\nqsWn747ONFxumofR/f/atqeDvM2QLfTerOySY5jN+uobzK0gewBl035ykzgMCHXU\nh7yz8/zJXa5+j1/blyNOgzpy2Ub5Ry6s17Haj8+1B/y4oSZIvkRtK6fTH0nvcQbm\nlOMNarLvvE57cjS/9RJ2kk2LZu8eW+GBvywahntQ7uIxG4PtS7d6W6rSWCGChrgh\nH4a2QIwbadrLMq5gLPM8qzMaVzDtea50AS5906YDUtRfkbSEXwhKFcPfYptfRAJR\nmu+wq08YuowYbG+CORwGskyH/18wNzJgwjC3VmQi3QKBgQDjNP577BYu3+zVg29w\n+DW56vjyJXdMDoSFvaE2I8EhMCJ38G6KSWcoOaN5Oqb8rssGtpfyAOf0dQLxc2qw\n396w4TyVyWFAZuIBcBuza+0bt6UjE4AUJ8jjCChoFMASW9jbNNrbXlA53LionJx+\ntEmQB5pRkRoNK5C4jySjAAlGlwKBgQDmooId8JGyPJmwjuOtejJekDPP6XL2/OJ7\nh9C0QHykKVJKalBPG2PFHDclXY2Cg+nanNpNXMRZVU/neyN6uSNFiYgtKXMHd4Ex\ntFgnQs0MMI6Tpo+isjhF6JbXCZyKUEhdFrf5B7GLdejvTMDiun2N5MTxvBI7RnCO\n+JUI+JcBrwKBgAml83SRtvNwoN8EQNQ8RhPKDZ5CxwkcyLXhMppY9FeTcrNDK36F\n1XKY1/9K5VJvncGAOX2WWkNAQMq+CvbN2ugJ+Ien0JBqjghfmV2KJLx7lPCjYFn6\nwoYZj5Wz4+AjtFbCrNSJ8cAzRkgqtl6PC1ypZf02uzN5+SBNO9IMK1irAoGAJpLN\nclZ297l89vOfDreeTwSNNdvUl4yKEKERfeQ/NHGYystnLSPmluP2MTCpZ0fKx/5t\n4HyAdnR3Tb7hmzf1tz6eYPdKvtf06qxABij9BGnmnrk/0rU+Bis1dzerT9LHl5Ii\nKOtpfWq2D7fllsYcE6xMaCXNYs6QKslWm85/6w8CgYAQKpwtxBXMJKs4mnE1Gtvz\nfKSnkKnimJNIgeYHRgmO9cC0XMseQywg8PMF8y7CA/xGzjalV3EhBQPiDuLyWWhk\n0b1PP1sLgyGLOhUERZjse4TVmpQK1Cd3kBbZTRdInD8U4t4FVF4YpbxDQyBCCP4U\nepW4oyX5nlYRW5n7zPGSbQ==\n-----END PRIVATE KEY-----"
    },
    "client2": {
        "client_id": "7f1fcf6a-2b4e-4e49-841f-c1aae1b7be75",
        "scope": "openid profile common:customer.basic:read energy:accounts.basic:read energy:accounts.concessions:read cdr:registration",
        "jwks": {
            "keys": [
                {
                    "alg": "PS256",
                    "e": "AQAB",
                    "use": "sig",
                    "kid": "C048AE076EB1DAAEB7F6139932AB143C5881E7F7",
                    "kty": "RSA",
                    "n": "zLHXp9xGE9zOF2vjRgQxrsSH-XZBIjxrW4O4CsoBLNaYlP3Z9U0mqsCXgfIUfaCDlbEu1PxvyW3MbM7yg5NIivrjpnRHEJH3-KWGa1R5YbN2Z9QQ3LlL5ZUl_9whrcCL8Scso2fJqnCT1wkGOsmmM75SOYhz8bLDuCuqZAycxhM6yFLrFYifQhr0AaekfPEcrB25I6zyAbEoNr2FS4xSZBM27BYCrtEqtIQxpP11N-uM-Qi5NfrAW0HrQIyLirjV241BsegGuCSw7rUdiHQocUpRwz3t3t4WN1EeCYKgM9nEV7BrippuTs8mOMyq5dxfhA98gy42PuTH1Mf_-q7YOQ",
                    "d": "b8ALz12neIKMlSbWbkwt3471-RBBYPKBXfXcTS-ZUqRZqsWn747ONFxumofR_f_atqeDvM2QLfTerOySY5jN-uobzK0gewBl035ykzgMCHXUh7yz8_zJXa5-j1_blyNOgzpy2Ub5Ry6s17Haj8-1B_y4oSZIvkRtK6fTH0nvcQbmlOMNarLvvE57cjS_9RJ2kk2LZu8eW-GBvywahntQ7uIxG4PtS7d6W6rSWCGChrghH4a2QIwbadrLMq5gLPM8qzMaVzDtea50AS5906YDUtRfkbSEXwhKFcPfYptfRAJRmu-wq08YuowYbG-CORwGskyH_18wNzJgwjC3VmQi3Q"
                },
                {
                    "alg": "RSA-OAEP",
                    "e": "AQAB",
                    "use": "enc",
                    "kid": "33eb40f41922f6109bb3c763a4166a3616a3f4d882e700f6ef7f380baadbb69d",
                    "kty": "RSA",
                    "n": "zLHXp9xGE9zOF2vjRgQxrsSH-XZBIjxrW4O4CsoBLNaYlP3Z9U0mqsCXgfIUfaCDlbEu1PxvyW3MbM7yg5NIivrjpnRHEJH3-KWGa1R5YbN2Z9QQ3LlL5ZUl_9whrcCL8Scso2fJqnCT1wkGOsmmM75SOYhz8bLDuCuqZAycxhM6yFLrFYifQhr0AaekfPEcrB25I6zyAbEoNr2FS4xSZBM27BYCrtEqtIQxpP11N-uM-Qi5NfrAW0HrQIyLirjV241BsegGuCSw7rUdiHQocUpRwz3t3t4WN1EeCYKgM9nEV7BrippuTs8mOMyq5dxfhA98gy42PuTH1Mf_-q7YOQ",
                    "d": "b8ALz12neIKMlSbWbkwt3471-RBBYPKBXfXcTS-ZUqRZqsWn747ONFxumofR_f_atqeDvM2QLfTerOySY5jN-uobzK0gewBl035ykzgMCHXUh7yz8_zJXa5-j1_blyNOgzpy2Ub5Ry6s17Haj8-1B_y4oSZIvkRtK6fTH0nvcQbmlOMNarLvvE57cjS_9RJ2kk2LZu8eW-GBvywahntQ7uIxG4PtS7d6W6rSWCGChrghH4a2QIwbadrLMq5gLPM8qzMaVzDtea50AS5906YDUtRfkbSEXwhKFcPfYptfRAJRmu-wq08YuowYbG-CORwGskyH_18wNzJgwjC3VmQi3Q"
                }
            ]
        }
    },
    "mtls2": {
        "cert": "-----BEGIN CERTIFICATE-----\nMIIDyjCCArKgAwIBAgIUEc4sqXy27pdUFFbkcnuuz/b37SQwDQYJKoZIhvcNAQEL\nBQAwajELMAkGA1UEBhMCQVUxDTALBgNVBAoMBEFDQ0MxDDAKBgNVBAsMA0NEUjEY\nMBYGA1UECwwPQ0RSIFNhbmRib3ggZGV2MSQwIgYDVQQDDBtDRFIgU2FuZGJveCBJ\nbnRlcm1lZGlhdGUgQ0EwHhcNMjIwMjE2MDM1MTA1WhcNMjMwMzIzMDM1MTA1WjBd\nMRAwDgYDVQQDDAdjbGllbnQyMQswCQYDVQQGEwJBVTEMMAoGA1UECAwDQUNUMREw\nDwYDVQQHDAhDYW5iZXJyYTENMAsGA1UECgwEQUNDQzEMMAoGA1UECwwDQ0RSMIIB\nIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAu35o551yUan0M8tH3F7e0zb8\nGupdiThdRa7Gulkuil50HK+WVE6TBgVIn0HQXVCGMGu5k2iuRfjVx2b7nEVirZbT\n9HRDAjwYqcW/9ntdT7dIIdv/gNPH+5VpxKQFWRsNV8v44mb2+f2vUG/Z5lPm4AQH\nCMwiYwpyg8B2eRQKOLy+jn45ujNhMEMCknOwlHcnrOIxac9QyjjDj8hevAyHP92q\nTiDx8+fGB5K6O7cOKB+peoqxyrrLRRxAv8bTGpkSlHuPLjHmDatB4zHknaPyUyfT\nYX8kLa4dAaqYhP5R3A2kXXyVkPVm4fpc4FOjZCQ9EFCGuYLRa4+cZpmZF/AZEwID\nAQABo3UwczAfBgNVHSMEGDAWgBTgomCukB9BCrLCterh2Mq73bXLIDAJBgNVHRME\nAjAAMA4GA1UdDwEB/wQEAwIF4DAWBgNVHSUBAf8EDDAKBggrBgEFBQcDAjAdBgNV\nHQ4EFgQUpa7cjy85phbXsQpA2uweK4pO3ggwDQYJKoZIhvcNAQELBQADggEBABYz\n8vLkKzAbSgqgDda1/Bp0XqZhSnx4bDI+yT6XK51V6HCsGMmTlWt+bRMz9jg/cwIK\nJVK4ZoN4PqqcuseaM7OSErzUw1/i/9V8Yh1M0sAUk+U3m0ZdecYM/gqieq+/rOuA\nEmjmTTVy8xAjg6nHvTG6JAEDmGT7yZCt2CFt3BTAn7PqiNKQhosxEfxdkUS1n1lA\nNGl+i/YnN9zu9vP+B+9+KWDpJ3gLRvmZsjyi1FFBSklTNzyUiF4DnxG3Ba3k/3kl\njQyNCJBGIY/6C9afCM2yoF3VhG8wHKBqZP/DNo/jcSyf2OOkWekrXpGZvvam+PsM\ngtkIgACNjW4a2pfEnOs=\n-----END CERTIFICATE-----",
        "key": "-----BEGIN PRIVATE KEY-----\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC7fmjnnXJRqfQz\ny0fcXt7TNvwa6l2JOF1Frsa6WS6KXnQcr5ZUTpMGBUifQdBdUIYwa7mTaK5F+NXH\nZvucRWKtltP0dEMCPBipxb/2e11Pt0gh2/+A08f7lWnEpAVZGw1Xy/jiZvb5/a9Q\nb9nmU+bgBAcIzCJjCnKDwHZ5FAo4vL6Ofjm6M2EwQwKSc7CUdyes4jFpz1DKOMOP\nyF68DIc/3apOIPHz58YHkro7tw4oH6l6irHKustFHEC/xtMamRKUe48uMeYNq0Hj\nMeSdo/JTJ9NhfyQtrh0BqpiE/lHcDaRdfJWQ9Wbh+lzgU6NkJD0QUIa5gtFrj5xm\nmZkX8BkTAgMBAAECggEACSbIOmHne2VHFk4aRv8vHh0ax2TgGuYFkMn784rjKCgo\namFcU5IgP/97BiGhaM9DitJ/P4WGWBj8tPEUX+d3w7mkb9P1lK2ClgAhI11DITR0\n0LObGtp7cjlel8AFGM58DhMr0JUuVzu9gQ4XPdROOmnyk/MuzH8X4XwVQZ9WSQ2W\ne9+DqtDcz7VIhpAqcXWlDEWTWVjkIB//F1i21Eg4WxCWTguWUXWupbslo5ojkl8c\nr4zjv+wQZ4oek2DOwBAA6JU15X0VSA8Xcl96PjfYDxxx8+2r4MXMkb6stqUeNUev\nlvYY8AvzVhaxxro34q06qWvpqHCfJAOnYDopSs0mtQKBgQD6BOU0na3ei46BIyeY\nx/O/xIaRXGAeJHg1qy3Ls13TeP/GWMh9qZfFhFhM84rdpIZYEj6CQ2ZmVDpWJnZi\nx4W/PirjzBwjGIFOSXfeLSBKO30uiO2fCiVIwqCWngaJiaHc79vhbjAKY+OwOnbw\nUCbR26HASTFfnu7VGdtrZ7er1QKBgQC/+pzHMh0aIglbm0jdV0N2RCei92TPP/w9\nHbhfA+dFmAcQBGZPa3U4aBaOuj7LtAB4lQBOrM/Cg3DQB4YTIKnZciEKWTd8TqWK\n9xY1oLK+tEvzcdq45T6I3luPfP7/NBXI5aE+ycYrw9Tf4tutQfiJXV8gXQI8Yd9K\nQ4KHTB8tRwKBgQDlc+yu1y8kmMuV9J94dblb+47MhQypXSr9hjYnRlwLonaKoByQ\nRz7ExOsM5E5Dj6TD2yqG/jhSHWbUfcQUb6xtkS5nlYEVLH4vTPm3a47A0cvXE0+Q\nsAz7s7MRx8GUJD3quC8ButBYGMhziZRyg/y8iGbwQ7wOV5w08uAOuEc2aQKBgChO\nthwcqX+TJePV9raCW+e455mP19qr1IoAc3V/nE9AXDtAsTp7lWECebn94LFkWbT3\nk9jw764nashCzCO39/FvxQAnOz8eRCOpPUCFPQJNWKUEgqfPehHCnfbCh8eNaAWG\nMRS9yJ3nwApB113JPCWbNR3WaWHEKt2szvsZQHKBAoGBALpd9iDVGGwtJc5Y5oZE\nRjKszDyevY9/DxrGRGU0SN4J0Hgz0+mmcotUFi+osJ0tePnDFBRmBfFwPOTSm8Us\n+EzWfs6jcmc2BkXNaed3kWyQrD9Z9t9Gkn7XWeTJQd6oXgFdQP8gX1k45Q+1ipOb\nF8vGfBTwMqI68RyGaVxlJLDY\n-----END PRIVATE KEY-----"
    },
    "resource": {
        "resourceUrl": "https://cdr-auth-server-dev.australiaeast.azurecontainer.io:5002/resource/cds-au/v1/common/customer",
        "cdrVersion": "1"
    },
    "consent": {}
}
```


