// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using IdentityServer4.WsFederation.Stores;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Security.Claims;

namespace IdentityServer4.WsFederation.Validation
{
    public class SignInValidationResult
    {
        public bool IsError => !string.IsNullOrWhiteSpace(Error);
        public string Error { get; set; }
        public string ErrorMessage { get; set; }

        public WsFederationMessage WsFederationMessage { get; set; }
        
        public ClaimsPrincipal User { get; set; }
        public bool SignInRequired { get; set; }

        public Client Client { get; set; }
        public RelyingParty RelyingParty { get; set; }

        public string ReplyUrl { get; set; } 
    }
}