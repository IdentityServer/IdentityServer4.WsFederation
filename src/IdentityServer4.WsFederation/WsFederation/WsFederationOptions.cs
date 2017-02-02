using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServer4.WsFederation
{
    public class WsFederationOptions
    {
        public string DefaultTokenType { get; set; } = WsFederationConstants.TokenTypes.Saml2TokenProfile11;
        public string DefaultDigestAlgorithm { get; set; } = SecurityAlgorithms.Sha256Digest;
        public string DefaultSignatureAlgorithm { get; set; } = SecurityAlgorithms.RsaSha256Signature;
        public string DefaultSamlNameIdentifierFormat { get; set; } = WsFederationConstants.SamlNameIdentifierFormats.UnspecifiedString;

        public IDictionary<string, string> DefaultClaimMapping { get; set; } = new Dictionary<string, string>
        {
            { "name", ClaimTypes.Name },
            { "sub", ClaimTypes.NameIdentifier },
            { "email", ClaimTypes.Email }
        };
    }
}