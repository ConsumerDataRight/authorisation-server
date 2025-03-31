﻿using CdrAuthServer.Configuration;
using CdrAuthServer.Models;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;

namespace CdrAuthServer.Validation
{
    public enum JwtValidationContext
    {
        [Display(Name = "client_assertion")]
        Client_assertion,
        [Display(Name = "access_token")]
        Access_token,
        [Display(Name = "request")]
        Request,
    }

    public interface IJwtValidator
    {
        /// <summary>
        /// Validates a JWT against the JWKS endpoint of the client.
        /// </summary>
        /// <param name="jwt">JWT to validate.</param>
        /// <param name="client">Client.</param>
        /// <param name="context">client_assertion, request or access_token.</param>
        /// <returns>ValidationResult and JwtSecurityToken.</returns>
        Task<(ValidationResult ValidationResult, JwtSecurityToken? JwtSecurityToken)> Validate(
            string jwt,
            Client client,
            JwtValidationContext context,
            ConfigurationOptions configOptions,
            IList<string>? validAudiences = null,
            IList<string>? validAlgorithms = null);
    }
}
