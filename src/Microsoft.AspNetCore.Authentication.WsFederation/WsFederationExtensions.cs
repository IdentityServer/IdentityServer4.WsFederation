// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WsFederationExtensions
    {
        public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder)
            => builder.AddWsFederation(WsFederationDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder, Action<WsFederationOptions> configureOptions)
            => builder.AddWsFederation(WsFederationDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder, string authenticationScheme, Action<WsFederationOptions> configureOptions)
            => builder.AddWsFederation(authenticationScheme, WsFederationDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<WsFederationOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<WsFederationOptions>, WsFederationPostConfigureOptions>());
            return builder.AddRemoteScheme<WsFederationOptions, WsFederationHandler>(authenticationScheme, displayName, configureOptions);
        }
    }
}
