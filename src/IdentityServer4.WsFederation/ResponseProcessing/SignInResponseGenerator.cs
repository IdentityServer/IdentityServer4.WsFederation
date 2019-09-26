// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.IdentityModel.Tokens.Saml2;

namespace IdentityServer4.WsFederation
{
    public class SignInResponseGenerator
    {
        private readonly IdentityServerOptions _options;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IProfileService _profile;
        private readonly IKeyMaterialService _keys;
        private readonly IResourceStore _resources;
        private readonly ILogger<SignInResponseGenerator> _logger;

        public SignInResponseGenerator(
            IHttpContextAccessor contextAccessor, 
            IdentityServerOptions options,
            IProfileService profile,
            IKeyMaterialService keys, 
            IResourceStore resources,
            ILogger<SignInResponseGenerator> logger)
        {
            _contextAccessor = contextAccessor;
            _options = options;
            _profile = profile;
            _keys = keys;
            _resources = resources;
            _logger = logger;
        }

        public async Task<WsFederationMessage> GenerateResponseAsync(SignInValidationResult validationResult)
        {
            _logger.LogDebug("Creating WS-Federation signin response");

            // create subject
            var outgoingSubject = await CreateSubjectAsync(validationResult);

            // create token for user
            var token = await CreateSecurityTokenAsync(validationResult, outgoingSubject);

            // return response
            return CreateResponse(validationResult, token);
        }

        protected async Task<ClaimsIdentity> CreateSubjectAsync(SignInValidationResult result)
        {
            var requestedClaimTypes = new List<string>();

            var resources = await _resources.FindEnabledIdentityResourcesByScopeAsync(result.Client.AllowedScopes);
            foreach (var resource in resources)
            {
                foreach (var claim in resource.UserClaims)
                {
                    requestedClaimTypes.Add(claim);
                }
            }

            var ctx = new ProfileDataRequestContext
            {
                Subject = result.User,
                RequestedClaimTypes = requestedClaimTypes,
                Client = result.Client,
                Caller = "WS-Federation"
            };

            await _profile.GetProfileDataAsync(ctx);
            
            // map outbound claims
            var nameid = new Claim(ClaimTypes.NameIdentifier, result.User.GetSubjectId());
            nameid.Properties[Microsoft.IdentityModel.Tokens.Saml.ClaimProperties.SamlNameIdentifierFormat] = result.RelyingParty.SamlNameIdentifierFormat;

            var outboundClaims = new List<Claim> { nameid };
            foreach (var claim in ctx.IssuedClaims)
            {
                if (result.RelyingParty.ClaimMapping.ContainsKey(claim.Type))
                {
                    var outboundClaim = new Claim(result.RelyingParty.ClaimMapping[claim.Type], claim.Value);
                    if (outboundClaim.Type == ClaimTypes.NameIdentifier)
                    {
                        outboundClaim.Properties[Microsoft.IdentityModel.Tokens.Saml.ClaimProperties.SamlNameIdentifierFormat] = result.RelyingParty.SamlNameIdentifierFormat;
                        outboundClaims.RemoveAll(c => c.Type == ClaimTypes.NameIdentifier); //remove previesly added nameid claim
                    }

                    outboundClaims.Add(outboundClaim);
                }
                else if (result.RelyingParty.TokenType != WsFederationConstants.TokenTypes.Saml11TokenProfile11)
                {
                    outboundClaims.Add(claim);
                }
                else
                {
                    _logger.LogInformation("No explicit claim type mapping for {claimType} configured. Saml11 requires a URI claim type. Skipping.", claim.Type);
                }
            }

            // The AuthnStatement statement generated from the following 2
            // claims is manditory for some service providers (i.e. Shibboleth-Sp). 
            // The value of the AuthenticationMethod claim must be one of the constants in
            // System.IdentityModel.Tokens.AuthenticationMethods.
            // Password is the only one that can be directly matched, everything
            // else defaults to Unspecified.
            if (result.User.GetAuthenticationMethod() == OidcConstants.AuthenticationMethods.Password)
            {
                outboundClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, SamlConstants.AuthenticationMethods.PasswordString));
            }
            else
            {
                outboundClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, SamlConstants.AuthenticationMethods.UnspecifiedString));
            }

            // authentication instant claim is required
            outboundClaims.Add(new Claim(ClaimTypes.AuthenticationInstant, XmlConvert.ToString(DateTime.UtcNow, "yyyy-MM-ddTHH:mm:ss.fffZ"), ClaimValueTypes.DateTime));

            return new ClaimsIdentity(outboundClaims, "idsrv");
        }

        private async Task<SecurityToken> CreateSecurityTokenAsync(SignInValidationResult result, ClaimsIdentity outgoingSubject)
        {
            var credential = await _keys.GetSigningCredentialsAsync();
            var key = credential.Key as Microsoft.IdentityModel.Tokens.X509SecurityKey; 
        
            var descriptor = new SecurityTokenDescriptor
            {
                Audience = result.Client.ClientId,
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddSeconds(result.Client.IdentityTokenLifetime),
                SigningCredentials = new SigningCredentials(key, result.RelyingParty.SignatureAlgorithm, result.RelyingParty.DigestAlgorithm),
                Subject = outgoingSubject,
                Issuer = _contextAccessor.HttpContext.GetIdentityServerIssuerUri(),
            };

            if (result.RelyingParty.EncryptionCertificate != null)
            {
                descriptor.EncryptingCredentials = new X509EncryptingCredentials(result.RelyingParty.EncryptionCertificate);
            }

            var handler = CreateTokenHandler(result.RelyingParty.TokenType);
            return handler.CreateToken(descriptor);
        }

        private WsFederationMessage CreateResponse(SignInValidationResult validationResult, SecurityToken token)
        {
            var handler = CreateTokenHandler(validationResult.RelyingParty.TokenType);
            var rstr = new RequestSecurityTokenResponse
            {
                CreatedAt = token.ValidFrom,
                ExpiresAt = token.ValidTo,
                AppliesTo = validationResult.Client.ClientId,
                Context = validationResult.WsFederationMessage.Wctx,
                ReplyTo = validationResult.ReplyUrl,
                RequestedSecurityToken = token,
                SecurityTokenHandler = handler,
            };
            var responseMessage = new WsFederationMessage {
                IssuerAddress = validationResult.Client.RedirectUris.First(),
                Wa = Microsoft.IdentityModel.Protocols.WsFederation.WsFederationConstants.WsFederationActions.SignIn,
                Wresult = rstr.Serialize(),
                Wctx = validationResult.WsFederationMessage.Wctx
            };
            return responseMessage;
        }

        private SecurityTokenHandler CreateTokenHandler(string tokenType)
        {
            switch (tokenType)
            {
                case WsFederationConstants.TokenTypes.Saml11TokenProfile11:
                    return new SamlSecurityTokenHandler();
                case WsFederationConstants.TokenTypes.Saml2TokenProfile11:
                    return new Saml2SecurityTokenHandler();
                default:
                    throw new NotImplementedException($"TokenType: {tokenType} not implemented");
            }
        }
    }
}
