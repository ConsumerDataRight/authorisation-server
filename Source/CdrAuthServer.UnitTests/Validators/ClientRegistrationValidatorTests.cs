using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.UnitTests.Validators
{
    public class ClientRegistrationValidatorTests : BaseTest
    {
        private Mock<ILogger<ClientRegistrationValidator>> logger;
        private Mock<IJwksService> jwksService;
        private IConfiguration configuration;
        private Mock<IHttpContextAccessor> httpContext;


        public SoftwareProduct softwareProduct { get; private set; }

        private IClientRegistrationValidator clientRegistrationValidator;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<ClientRegistrationValidator>>();

            jwksService = new Mock<IJwksService>();

            httpContext = new Mock<IHttpContextAccessor>();

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.UnitTest.json")
                 .AddEnvironmentVariables()
                 .Build();

            softwareProduct = new SoftwareProduct();
            softwareProduct.SoftwareProductId = configuration[SOFTWARE_PRODUCT_ID_KEY];
            softwareProduct.BrandId = configuration[BRAND_ID_KEY];

            clientRegistrationValidator = new ClientRegistrationValidator(configuration, logger.Object, jwksService.Object, httpContext.Object);
        }


        //TODO
        // Assert - Validate SSA Signature        
        [TestCase("empty_client_registration", false, "Registration request is empty", ErrorCodes.InvalidClientMetadata)]
        [TestCase("SSA_validation_without_SS", false, "The software_statement is empty or invalid", ErrorCodes.InvalidSoftwareStatement)]
        [TestCase("SSA_validation_with_SS", false, "Could not load SSA JWKS from Register endpoint: "+ JWKS_URI, ErrorCodes.InvalidSoftwareStatement)]
        [TestCase("SSA_validation_with_SS_and_Null_JWKS", false, "Could not load SSA JWKS from Register endpoint: "+ JWKS_URI, ErrorCodes.InvalidSoftwareStatement)]
        [TestCase("SSA_validation_with_SS_and_JWKS", false, "SSA validation failed.", ErrorCodes.InvalidSoftwareStatement)]        
        public async Task Validate_ClientRegistrationRequest_InvalidClient_Test(string testCaseType, bool isvalid, 
                                                                                string expectErrorDescription,  
                                                                                string expectedError) 
        {
            //Arrange
            var configOptions = this.configuration.GetConfigurationOptions();
            ClientRegistrationRequest clientRegistrationRequest = null;

            if (String.Equals("empty_client_registration", testCaseType))
            {
                clientRegistrationRequest = null;
            }
            if (String.Equals("SSA_validation_without_SS", testCaseType))
            {
                clientRegistrationRequest = new ClientRegistrationRequest(GetJwtToken());
            }
            if (String.Equals("SSA_validation_with_SS", testCaseType))
            {
                clientRegistrationRequest = new ClientRegistrationRequest(GetJwtToken(requireSS:true));                
                //SoftwareStatementJwt is readonly
                //clientRegistrationRequest.SoftwareStatementJwt;

                var softwareStatement = new SoftwareStatement(clientRegistrationRequest.SoftwareStatementJwt);
                _ = softwareStatement.ValidFrom;
                _ = softwareStatement.ValidTo;
                clientRegistrationRequest.SoftwareStatement = softwareStatement;                
            }
            if (String.Equals("SSA_validation_with_SS_and_Null_JWKS", testCaseType))
            {
                clientRegistrationRequest = new ClientRegistrationRequest(GetJwtToken(requireSS: true));                
                var softwareStatement = new SoftwareStatement(clientRegistrationRequest.SoftwareStatementJwt);
                _ = softwareStatement.ValidFrom;
                _ = softwareStatement.ValidTo;
                clientRegistrationRequest.SoftwareStatement = softwareStatement;
                
                var jwksURI = new Uri(JWKS_URI);
                jwksService.Setup(x => x.GetJwks(jwksURI)).ReturnsAsync(value: null as Microsoft.IdentityModel.Tokens.JsonWebKeySet);
            }
            if (String.Equals("SSA_validation_with_SS_and_JWKS", testCaseType))
            {
                clientRegistrationRequest = new ClientRegistrationRequest(GetJwtToken(requireSS: true));
                var softwareStatement = new SoftwareStatement(clientRegistrationRequest.SoftwareStatementJwt);
                _ = softwareStatement.ValidFrom;
                _ = softwareStatement.ValidTo;
                clientRegistrationRequest.SoftwareStatement = softwareStatement;
                
                var jwksURI = new Uri(JWKS_URI);
                Microsoft.IdentityModel.Tokens.JsonWebKeySet jsonWebKeySet =  new Microsoft.IdentityModel.Tokens.JsonWebKeySet();
                jsonWebKeySet.Keys.Add(new Microsoft.IdentityModel.Tokens.JsonWebKey());
                jwksService.Setup(x => x.GetJwks(jwksURI, softwareStatement.Header["kid"].ToString())).ReturnsAsync(jsonWebKeySet);
            }

            //Act
            var result = await clientRegistrationValidator.Validate(clientRegistrationRequest, configOptions);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(result.IsValid, isvalid);
            Assert.AreEqual(result.Error, expectedError);            
            Assert.IsTrue(result.ErrorDescription.Contains(expectErrorDescription));
        }
    }
}
