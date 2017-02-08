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
using System;
using System.Collections.Generic;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Services;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml;

namespace IdentityServer4.WsFederation
{
    public class SignInResponseGenerator
    {
        private readonly IdentityServerOptions _options;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IProfileService _profile;
        private readonly IKeyMaterialService _keys;
        private readonly IResourceStore _resources;

        public SignInResponseGenerator(
            IHttpContextAccessor contextAccessor, 
            IdentityServerOptions options,
            IProfileService profile,
            IKeyMaterialService keys, 
            IResourceStore resources)
        {
            _contextAccessor = contextAccessor;
            _options = options;
            _profile = profile;
            _keys = keys;
            _resources = resources;
        }

        public async Task<SignInResponseMessage> GenerateResponseAsync(SignInValidationResult validationResult)
        {
            //Logger.Info("Creating WS-Federation signin response");

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
            nameid.Properties[ClaimProperties.SamlNameIdentifierFormat] = result.RelyingParty.SamlNameIdentifierFormat;

            var outboundClaims = new List<Claim> { nameid };
            foreach (var claim in ctx.IssuedClaims)
            {
                if (result.RelyingParty.ClaimMapping.ContainsKey(claim.Type))
                {
                    var outboundClaim = new Claim(result.RelyingParty.ClaimMapping[claim.Type], claim.Value);
                    if (outboundClaim.Type == ClaimTypes.NameIdentifier)
                    {
                        outboundClaim.Properties[ClaimProperties.SamlNameIdentifierFormat] = result.RelyingParty.SamlNameIdentifierFormat;
                    }

                    outboundClaims.Add(outboundClaim);
                }
                else
                {
                    outboundClaims.Add(claim);
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
                outboundClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, AuthenticationMethods.Password));
            }
            else
            {
                outboundClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, AuthenticationMethods.Unspecified));
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
                AppliesToAddress = result.Client.ClientId,
                Lifetime = new Lifetime(DateTime.UtcNow, DateTime.UtcNow.AddSeconds(result.Client.IdentityTokenLifetime)),
                ReplyToAddress = result.Client.RedirectUris.First(),
                SigningCredentials = new X509SigningCredentials(key.Certificate, result.RelyingParty.SignatureAlgorithm, result.RelyingParty.DigestAlgorithm),
                Subject = outgoingSubject,
                TokenIssuerName = _contextAccessor.HttpContext.GetIdentityServerIssuerUri(),
                TokenType = result.RelyingParty.TokenType
            };

            if (result.RelyingParty.EncryptionCertificate != null)
            {
                descriptor.EncryptingCredentials = new EncryptedKeyEncryptingCredentials(result.RelyingParty.EncryptionCertificate);
            }

            return CreateSupportedSecurityTokenHandler().CreateToken(descriptor);
        }

        private SignInResponseMessage CreateResponse(SignInValidationResult validationResult, SecurityToken token)
        {
            var rstr = new RequestSecurityTokenResponse
            {
                AppliesTo = new EndpointReference(validationResult.Client.ClientId),
                Context = validationResult.SignInRequestMessage.Context,
                ReplyTo = validationResult.ReplyUrl,
                RequestedSecurityToken = new RequestedSecurityToken(token)
            };

            var serializer = new WSFederationSerializer(
                new WSTrust13RequestSerializer(),
                new WSTrust13ResponseSerializer());

            var mgr = SecurityTokenHandlerCollectionManager.CreateEmptySecurityTokenHandlerCollectionManager();
            mgr[SecurityTokenHandlerCollectionManager.Usage.Default] = CreateSupportedSecurityTokenHandler();

            var responseMessage = new SignInResponseMessage(
                new Uri(validationResult.ReplyUrl),
                rstr,
                serializer,
                new WSTrustSerializationContext(mgr));

            return responseMessage;
        }

        private SecurityTokenHandlerCollection CreateSupportedSecurityTokenHandler()
        {
            return new SecurityTokenHandlerCollection(new SecurityTokenHandler[]
            {
                new SamlSecurityTokenHandler(),
                new EncryptedSecurityTokenHandler(),
                new Saml2SecurityTokenHandler()
            });
        }
    }
}
