using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Infrastructure.Certificates;
using CdrAuthServer.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Org.BouncyCastle.Security.Certificates;
using System.Configuration;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CdrAuthServer.Validation
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ValidateMtlsAttribute : ActionFilterAttribute
    {
        private readonly ILogger<ValidateMtlsAttribute> _logger;
        private readonly IConfiguration _configuration;
        //private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;

        public ValidateMtlsAttribute(ILogger<ValidateMtlsAttribute> logger, IConfiguration configuration, /*IMemoryCache cache,*/ HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            //_cache = cache;
            _httpClient = httpClient;
        }

        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var configOptions = _configuration.GetConfigurationOptions();

            if (configOptions.OverrideMtlsChecks.Contains(context.HttpContext.Request.Path))
            {
                _logger.LogDebug("Overriding mtls check for {path}...", context.HttpContext.Request.Path);
                base.OnActionExecuting(context);
                return;
            }

            if (_configuration.GetValue<bool>("Certificates:Ocsp:Enabled") == true)
            {
                VerifyCertificateRevocation(context, configOptions);
            }

            if (context.HttpContext?.Request.Headers.TryGetValue(configOptions.ClientCertificateThumbprintHttpHeaderName, out StringValues headerThumbprints) is true)
            {
                if (headerThumbprints.Count > 1)
                {
                    _logger.LogError("Multiple client certificate thumbprints found in request header");
                    context.Result = ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.MTLS_MULTIPLE_THUMBPRINTS);
                }
            }
            else
            {
                _logger.LogError("No client certificate found in request header");
                context.Result = ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.MTLS_NO_CERTIFICATE);
            }

            // Client certificate ok.
            base.OnActionExecuting(context);
        }

        private void VerifyCertificateRevocation(ActionExecutingContext context, ConfigurationOptions configOptions)
        {
            if (context.HttpContext?.Request.Headers.TryGetValue(configOptions.ClientCertificateHttpHeaderName, out StringValues clientCerts) is true)
            {
                if (clientCerts.Count > 0)
                {
                    _logger.LogInformation("Cert received for OCSP - {cert}", clientCerts[0]);

                    var processedCertString = OcspRequester.GetBytesFromPEM(WebUtility.UrlDecode(clientCerts[0]));

                    _logger.LogInformation("Cert after cleanup for OCSP - {processed}", processedCertString);

                    // Load the certificate into an X509Certificate object.
                    var cert = new X509Certificate2(processedCertString);

                    try
                    {
                        // Build the OCSP request URL from the client cert
                        var ocspResponderUrl = cert.GetOCSPUrlFromCertificate();
                        _logger.LogInformation("OCSP Responder URL: {url}", ocspResponderUrl);

                        
                        //Read the CA PEM from configuration.
                        var clientCertCAPem = _configuration.GetValue<string>("Certificates:Ocsp:MtlsOcspResponderPem");

                        if (string.IsNullOrEmpty(clientCertCAPem))
                        {
                            _logger.LogError("Certificates:Ocsp:MtlsOcspResponderPem value is either null or empty");
                            throw new ConfigurationErrorsException("Certificates:Ocsp:MtlsOcspResponderPem value is either null or empty");
                        }

                        //create request object for ocsp.
                        var ocspRequester = new OcspRequester(ocspResponderUrl, clientCertCAPem, _logger, _httpClient);

                        _logger.LogInformation("mTLS certificate check - calling OCSP Responder at {ocspResponderUrl}", ocspResponderUrl);

                        // Call the OCSP responder to get the status of the certificate.
                        var ocspResult = ocspRequester.GetResult(cert.GetSerialNumberString()).Result;

                        _logger.LogInformation("mTLS certificate check - OCSP Response for {serialNumber} = {ocspResult}", cert.GetSerialNumberString(), ocspResult);

                        if (ocspResult != OcspRequester.OcspResult.Good)
                        {
                            _logger.LogError("OCSP check failed for cert");
                            var ocspError = ErrorCatalogue.Catalogue().GetErrorDefinition(ErrorCatalogue.MTLS_CERT_OCSP_FAILED);
                            var error = new Models.Error(ocspError.Error, string.Format(ocspError.ErrorDescription, cert.GetSerialNumberString(), ocspResult));
                            context.Result = new JsonResult(error) { StatusCode = ocspError.StatusCode };
                        }
                    }
                    catch (CertificateException certException) 
                    {
                        _logger.LogError($"{certException.Message}", certException);
                        var ocspError = ErrorCatalogue.Catalogue().GetErrorDefinition(ErrorCatalogue.MTLS_CERT_OCSP_ERROR);
                        var error = new Models.Error(ocspError.Error, string.Format(ocspError.ErrorDescription, certException.Message));
                        context.Result = new JsonResult(error) { StatusCode = ocspError.StatusCode };
                    }                    
                }
            }
        }
    }
}
