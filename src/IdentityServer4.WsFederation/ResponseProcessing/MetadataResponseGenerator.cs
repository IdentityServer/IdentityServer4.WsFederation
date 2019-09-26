// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;
using IdentityServer4.Extensions;
using System.Collections.Generic;
using Microsoft.IdentityModel.Xml;

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

        public async Task<WsFederationConfiguration> GenerateAsync(string wsfedEndpoint)
        {
            var signingKey = (await _keys.GetSigningCredentialsAsync()).Key as X509SecurityKey;
            var cert = signingKey.Certificate;
            var issuer = _contextAccessor.HttpContext.GetIdentityServerIssuerUri();
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256Signature, SecurityAlgorithms.Sha256Digest);
            var config = new WsFederationConfiguration()
            {
                Issuer = issuer,
                TokenEndpoint = wsfedEndpoint,
                SigningCredentials = signingCredentials,
            };
            config.SigningKeys.Add(signingKey);
            config.KeyInfos.Add(new KeyInfo(cert));

            return config;
        }
    }
}