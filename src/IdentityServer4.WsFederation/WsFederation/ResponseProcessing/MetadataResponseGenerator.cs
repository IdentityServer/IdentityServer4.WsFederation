// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Metadata;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using IdentityServer4.Extensions;

namespace IdentityServer4.WsFederation
{
    public class MetadataResponseGenerator
    {
        private readonly IKeyMaterialService _keys;
        private readonly IHttpContextAccessor _contextAccessor;

        public MetadataResponseGenerator(IHttpContextAccessor contextAccessor, IKeyMaterialService keys)
        {
            _keys = keys;
            _contextAccessor = contextAccessor;
        }

        public async Task<EntityDescriptor> GenerateAsync(string wsfedEndpoint)
        {
            var signingKey = (await _keys.GetSigningCredentialsAsync()).Key as X509SecurityKey;
            var cert = signingKey.Certificate;

            var applicationDescriptor = GetApplicationDescriptor(wsfedEndpoint, cert);
            var tokenServiceDescriptor = GetTokenServiceDescriptor(wsfedEndpoint, cert);

            var id = new EntityId(_contextAccessor.HttpContext.GetIdentityServerIssuerUri());
            var entity = new EntityDescriptor(id);

            entity.SigningCredentials = new X509SigningCredentials(cert);
            entity.RoleDescriptors.Add(applicationDescriptor);
            entity.RoleDescriptors.Add(tokenServiceDescriptor);

            return entity;
        }

        private SecurityTokenServiceDescriptor GetTokenServiceDescriptor(string wsfedEndpoint, X509Certificate2 cert)
        {
            var tokenService = new SecurityTokenServiceDescriptor();
            tokenService.ServiceDescription = "IdentityServer4 WS-Federation Endpoint";
            tokenService.Keys.Add(GetSigningKeyDescriptor(cert));
            
            tokenService.PassiveRequestorEndpoints.Add(new EndpointReference(wsfedEndpoint));
            tokenService.SecurityTokenServiceEndpoints.Add(new EndpointReference(wsfedEndpoint));

            tokenService.TokenTypesOffered.Add(new Uri("http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1"));
            tokenService.TokenTypesOffered.Add(new Uri("http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0"));
            tokenService.ProtocolsSupported.Add(new Uri("http://docs.oasis-open.org/wsfed/federation/200706"));

            return tokenService;
        }

        private ApplicationServiceDescriptor GetApplicationDescriptor(string wsfedEndpoint, X509Certificate2 cert)
        {
            var tokenService = new ApplicationServiceDescriptor();
            tokenService.ServiceDescription = "poc";
            tokenService.Keys.Add(GetEncryptionDescriptor(cert));
            tokenService.Keys.Add(GetSigningKeyDescriptor(cert));

            tokenService.PassiveRequestorEndpoints.Add(new EndpointReference(wsfedEndpoint));
            tokenService.Endpoints.Add(new EndpointReference(wsfedEndpoint));

            tokenService.TokenTypesOffered.Add(new Uri("http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1"));
            tokenService.TokenTypesOffered.Add(new Uri("http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0"));
            tokenService.ProtocolsSupported.Add(new Uri("http://docs.oasis-open.org/wsfed/federation/200706"));

            return tokenService;
        }

        private KeyDescriptor GetSigningKeyDescriptor(X509Certificate2 certificate)
        {
            var clause = new X509SecurityToken(certificate).CreateKeyIdentifierClause<X509RawDataKeyIdentifierClause>();
            var key = new KeyDescriptor(new SecurityKeyIdentifier(clause));
            key.Use = KeyType.Signing;

            return key;
        }

        private KeyDescriptor GetEncryptionDescriptor(X509Certificate2 certificate)
        {
            var clause = new X509SecurityToken(certificate).CreateKeyIdentifierClause<X509RawDataKeyIdentifierClause>();
            var key = new KeyDescriptor(new SecurityKeyIdentifier(clause));
            key.Use = KeyType.Encryption;

            return key;
        }
    }
}