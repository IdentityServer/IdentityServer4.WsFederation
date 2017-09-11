// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
    /// <summary>
    /// Default values related to WsFederation authentication handler
    /// </summary>
    public static class WsFederationDefaults
    {
        public const string AuthenticationScheme = "Federation";

        /// <summary>
        /// The prefix used to provide a default WsFederationAuthenticationOptions.CookieName
        /// </summary>
        public const string CookiePrefix = "WsFederation.";

        /// <summary>
        /// The prefix used to provide a default WsFederationAuthenticationOptions.CookieName
        /// </summary>
        public const string CookieName = "WsFederationAuth";
        
        public const string DisplayName = "WsFederation";

        internal const string WctxKey = "WsFedOwinState";
    }
}
