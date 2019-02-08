// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServer4.WsFederation
{
    public class WsFederationOptions
    {
        public string DefaultTokenType { get; set; } = WsFederationConstants.TokenTypes.Saml11TokenProfile11;
        public string DefaultDigestAlgorithm { get; set; } = SecurityAlgorithms.Sha256Digest;
        public string DefaultSignatureAlgorithm { get; set; } = SecurityAlgorithms.RsaSha256Signature;
        public string DefaultSamlNameIdentifierFormat { get; set; } = WsFederationConstants.SamlNameIdentifierFormats.UnspecifiedString;

        public IDictionary<string, string> DefaultClaimMapping { get; set; } = new Dictionary<string, string>
        {
            { JwtClaimTypes.Name, ClaimTypes.Name },
            { JwtClaimTypes.Subject, ClaimTypes.NameIdentifier },
            { JwtClaimTypes.Email, ClaimTypes.Email },
            { JwtClaimTypes.GivenName, ClaimTypes.GivenName },
            { JwtClaimTypes.FamilyName, ClaimTypes.Surname },
            { JwtClaimTypes.BirthDate, ClaimTypes.DateOfBirth },
            { JwtClaimTypes.WebSite, ClaimTypes.Webpage },
            { JwtClaimTypes.Gender, ClaimTypes.Gender },
            { JwtClaimTypes.Role, ClaimTypes.Role }
        };
    }
}