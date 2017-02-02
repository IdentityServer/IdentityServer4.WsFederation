// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace IdentityServer4.WsFederation.Stores
{ 
    public class RelyingParty
    {
        public string Realm { get; set; }

        public string TokenType { get; set; }
        public string DigestAlgorithm { get; set; }
        public string SignatureAlgorithm { get; set; }
        public string SamlNameIdentifierFormat { get; set; }
        public X509Certificate2 EncryptionCertificate { get; set; }

        public IDictionary<string, string> ClaimMapping { get; set; } = new Dictionary<string, string>();
    }
}