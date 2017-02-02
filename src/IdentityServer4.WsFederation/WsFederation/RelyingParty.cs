using System.Collections.Generic;

namespace IdentityServer4.WsFederation
{ 
    public class RelyingParty
    {
        public string Realm { get; set; }

        public string TokenType { get; set; }
        public string DigestAlgorithm { get; set; }
        public string SignatureAlgorithm { get; set; }
        public string SamlNameIdentifierFormat { get; set; }

        public IDictionary<string, string> DefaultClaimMapping { get; set; } = new Dictionary<string, string>();
    }
}