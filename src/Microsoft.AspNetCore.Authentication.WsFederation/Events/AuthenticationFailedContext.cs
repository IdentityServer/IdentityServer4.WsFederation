// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
    public class AuthenticationFailedContext : RemoteAuthenticationContext<WsFederationOptions>
    {
        public AuthenticationFailedContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options)
            : base(context, scheme, options, new AuthenticationProperties())
        { }

        public WsFederationMessage ProtocolMessage { get; set; }

        public Exception Exception { get; set; }
    }
}