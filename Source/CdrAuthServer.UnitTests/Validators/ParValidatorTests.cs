using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.UnitTests.Validators
{
    public class ParValidatorTest : BaseTest
    {
        private Mock<ILogger<ParValidator>> logger;        
        private Mock<IClientService> clientService;

        private Mock<IJwtValidator> jwtValidator;
        private Mock<IRequestObjectValidator> requestObjectValidator;

        private IConfiguration configuration;
        
        private IParValidator parValidator;

        public Client client { get; private set; }
        private (ValidationResult, JwtSecurityToken) mockJwtValidatorResult;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<ParValidator>>();

            clientService = new Mock<IClientService>();
            jwtValidator = new Mock<IJwtValidator>();
            requestObjectValidator = new Mock<IRequestObjectValidator>();   

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.UnitTest.json")
                 .AddEnvironmentVariables()
                 .Build();

            parValidator = new ParValidator(logger.Object, jwtValidator.Object, requestObjectValidator.Object, clientService.Object);
        }
        
        [TestCase("par_request_client_id_missing", "", "", false, "request is not a well-formed JWT", ErrorCodes.InvalidRequest)]
        [TestCase("par_request_client_invalid_jwt", "foo", "", false, "", ErrorCodes.InvalidRequestObject)]
        [TestCase("par_request_client_jwt_validator", "foo", "", false, "", ErrorCodes.InvalidRequestObject)]        
        public async Task Validate_Par_Request_InvalidClient_Test(string testCaseType, string client_id, string requestObject, 
                                                                bool isvalid,
                                                                string expectErrorDescription,  
                                                                string expectedError) 
        {
            //Arrange
            var configOptions = this.configuration.GetConfigurationOptions();
            if (String.Equals("par_request_client_invalid_jwt", testCaseType))
            {
                requestObject = GetJwtToken();
                GetClient(client_id);
            }
            if (String.Equals("par_request_client_jwt_validator", testCaseType))
            {
                requestObject = GetJwtToken();
                GetClient(client_id);
                
                var audiences = new List<string>() { "https://localhost:8081", "https://localhost:8082/connect/token" };
                var validAlgos = new List<string>() { "PS256", "ES256" };                
                ValidationResult validationResult = new ValidationResult(isvalid);
                
                mockJwtValidatorResult = (validationResult, GetJwt());                
                jwtValidator.Setup(x => x.Validate(requestObject, client, JwtValidationContext.request, configOptions, audiences, validAlgos)).ReturnsAsync(mockJwtValidatorResult);
            }
            
            //Act
            var result = await parValidator.Validate(client_id, requestObject, configOptions);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Item1.IsValid, isvalid);            
            Assert.AreEqual(result.Item1.Error, expectedError);            
            Assert.IsTrue(result.Item1.ErrorDescription.Contains(expectErrorDescription));
        }

        private void GetClient(string client_id)
        {
            client = new Client();
            client.ClientId = client_id;
            clientService.Setup(x => x.Get(client_id)).ReturnsAsync(client);
        }
    }
}
