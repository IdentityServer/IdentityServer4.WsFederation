using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation
{
    public static class SamlNameIdentifierFormats
    {
        public const string EmailAddressString = "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress";
        public const string EncryptedString = "urn:oasis:names:tc:SAML:2.0:nameid-format:encrypted";
        public const string EntityString = "urn:oasis:names:tc:SAML:2.0:nameid-format:entity";
        public const string KerberosString = "urn:oasis:names:tc:SAML:2.0:nameid-format:kerberos";
        public const string PersistentString = "urn:oasis:names:tc:SAML:2.0:nameid-format:persistent";
        public const string TransientString = "urn:oasis:names:tc:SAML:2.0:nameid-format:transient";
        public const string UnspecifiedString = "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified";
        public const string WindowsDomainQualifiedNameString = "urn:oasis:names:tc:SAML:1.1:nameid-format:WindowsDomainQualifiedName";
        public const string X509SubjectNameString = "urn:oasis:names:tc:SAML:1.1:nameid-format:X509SubjectName";
    }

    public static class TokenTypes
    {
        public const string JsonWebToken = "urn:ietf:params:oauth:token-type:jwt";
        public const string Kerberos = "http://schemas.microsoft.com/ws/2006/05/identitymodel/tokens/Kerberos";
        public const string OasisWssSaml11TokenProfile11 = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1";
        public const string OasisWssSaml2TokenProfile11 = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0";
        public const string Rsa = "http://schemas.microsoft.com/ws/2006/05/identitymodel/tokens/Rsa";
        public const string Saml11TokenProfile11 = "urn:oasis:names:tc:SAML:1.0:assertion";
        public const string Saml2TokenProfile11 = "urn:oasis:names:tc:SAML:2.0:assertion";
        public const string SimpleWebToken = "http://schemas.xmlsoap.org/ws/2009/11/swt-token-profile-1.0";
        public const string UserName = "http://schemas.microsoft.com/ws/2006/05/identitymodel/tokens/UserName";
        public const string X509Certificate = "http://schemas.microsoft.com/ws/2006/05/identitymodel/tokens/X509Certificate";
    }

    public class WsFederationOptions
    {
        public string DefaultTokenType { get; set; } = TokenTypes.Saml2TokenProfile11;
        public string DefaultDigestAlgorithm { get; set; } = SecurityAlgorithms.Sha256Digest;
        public string DefaultSignatureAlgorithm { get; set; } = SecurityAlgorithms.RsaSha256Signature;
        public string DefaultSamlNameIdentifierFormat { get; set; } = SamlNameIdentifierFormats.UnspecifiedString;

        public IDictionary<string, string> DefaultClaimMapping { get; set; } = new Dictionary<string, string>
        {
            { "name", ClaimTypes.Name },
            { "sub", ClaimTypes.NameIdentifier },
            { "email", ClaimTypes.Email }
        };
    }
}