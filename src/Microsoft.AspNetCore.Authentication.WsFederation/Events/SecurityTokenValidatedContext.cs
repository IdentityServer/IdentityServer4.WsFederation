// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
    public class SecurityTokenValidatedContext : RemoteAuthenticationContext<WsFederationOptions>
    {
        /// <summary>
        /// Creates a <see cref="SecurityTokenValidatedContext"/>
        /// </summary>
        public SecurityTokenValidatedContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options, ClaimsPrincipal principal, AuthenticationProperties properties)
            : base(context, scheme, options, properties)
            => Principal = principal;

        public WsFederationMessage ProtocolMessage { get; set; }

        public JwtSecurityToken SecurityToken { get; set; }

        public WsFederationMessage TokenEndpointResponse { get; set; }

        public string Nonce { get; set; }
    }
}