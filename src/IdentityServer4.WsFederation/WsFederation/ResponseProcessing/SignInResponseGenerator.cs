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
        //private readonly static ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly IdentityServerOptions _options;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IProfileService _profile;
        private readonly IKeyMaterialService _keys;
        private readonly WsFederationOptions _wsfedOptions;
        private readonly IResourceStore _resources;

        public SignInResponseGenerator(
            IHttpContextAccessor contextAccessor, 
            IdentityServerOptions options,
            WsFederationOptions wsfedOptions, 
            IProfileService profile,
            IKeyMaterialService keys, 
            IResourceStore resources)
        {
            _contextAccessor = contextAccessor;
            _options = options;
            _wsfedOptions = wsfedOptions;
            _profile = profile;
            _keys = keys;
            _resources = resources;
        }

        private string IssuerUri
        {
            get
            {
                return _contextAccessor.HttpContext.GetIdentityServerIssuerUri();
            }
        }

        public async Task<SignInResponseMessage> GenerateResponseAsync(SignInValidationResult validationResult)
        {
            //Logger.Info("Creating WS-Federation signin response");

            // create subject
            var outgoingSubject = await CreateSubjectAsync(validationResult);

            // create token for user
            var token = await CreateSecurityTokenAsync(validationResult, outgoingSubject);

            // return response
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

        protected async Task<ClaimsIdentity> CreateSubjectAsync(SignInValidationResult validationResult)
        {
            var profileClaims = new List<Claim>();
            var requestedClaimTypes = new List<string>();

            var resources = await _resources.FindEnabledIdentityResourcesByScopeAsync(validationResult.Client.AllowedScopes);
            foreach (var resource in resources)
            {
                foreach (var claim in resource.UserClaims)
                {
                    requestedClaimTypes.Add(claim);
                }
            }

            var ctx = new ProfileDataRequestContext
            {
                Subject = validationResult.User,
                RequestedClaimTypes = requestedClaimTypes,
                Client = validationResult.Client,
                Caller = "WS-Federation"
            };

            await _profile.GetProfileDataAsync(ctx);
            profileClaims = ctx.IssuedClaims.ToList();

            // map outbound claims
            var nameid = new Claim(ClaimTypes.NameIdentifier, validationResult.User.GetSubjectId());
            nameid.Properties[ClaimProperties.SamlNameIdentifierFormat] = _wsfedOptions.DefaultSamlNameIdentifierFormat;

            var outboundClaims = new List<Claim> { nameid };
            
            foreach (var claim in profileClaims)
            {
                if (_wsfedOptions.DefaultClaimMapping.ContainsKey(claim.Type))
                {
                    var outboundClaim = new Claim(_wsfedOptions.DefaultClaimMapping[claim.Type], claim.Value);
                    if (outboundClaim.Type == ClaimTypes.NameIdentifier)
                    {
                        outboundClaim.Properties[ClaimProperties.SamlNameIdentifierFormat] = _wsfedOptions.DefaultSamlNameIdentifierFormat;
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
            if (validationResult.User.GetAuthenticationMethod() == OidcConstants.AuthenticationMethods.Password)
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

        private async Task<System.IdentityModel.Tokens.SecurityToken> CreateSecurityTokenAsync(SignInValidationResult validationResult, ClaimsIdentity outgoingSubject)
        {
            var credential = await _keys.GetSigningCredentialsAsync();
            var key = credential.Key as Microsoft.IdentityModel.Tokens.X509SecurityKey; 
        
            var descriptor = new System.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                AppliesToAddress = validationResult.Client.ClientId,
                Lifetime = new Lifetime(DateTime.UtcNow, DateTime.UtcNow.AddSeconds(validationResult.Client.IdentityTokenLifetime)),
                ReplyToAddress = validationResult.Client.RedirectUris.First(),
                SigningCredentials = new X509SigningCredentials(key.Certificate, _wsfedOptions.DefaultSignatureAlgorithm, _wsfedOptions.DefaultDigestAlgorithm),
                Subject = outgoingSubject,
                TokenIssuerName = IssuerUri,
                TokenType = _wsfedOptions.DefaultTokenType
            };

            //if (validationResult.RelyingParty.EncryptingCertificate != null)
            //{
            //    descriptor.EncryptingCredentials = new EncryptedKeyEncryptingCredentials(validationResult.RelyingParty.EncryptingCertificate);
            //}

            return CreateSupportedSecurityTokenHandler().CreateToken(descriptor);
        }

        private SecurityTokenHandlerCollection CreateSupportedSecurityTokenHandler()
        {
            return new SecurityTokenHandlerCollection(new System.IdentityModel.Tokens.SecurityTokenHandler[]
            {
                new SamlSecurityTokenHandler(),
                new EncryptedSecurityTokenHandler(),
                new Saml2SecurityTokenHandler()
            });
        }
    }
}
