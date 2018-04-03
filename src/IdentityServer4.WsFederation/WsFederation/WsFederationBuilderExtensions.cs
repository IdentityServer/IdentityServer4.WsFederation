// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Services;
using IdentityServer4.WsFederation;
using IdentityServer4.WsFederation.Stores;
using IdentityServer4.WsFederation.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WsFederationBuilderExtensions
    {
        public static IIdentityServerBuilder AddWsFederation(this IIdentityServerBuilder builder)
        {
            builder.Services.AddTransient<MetadataResponseGenerator>();
            builder.Services.AddTransient<SignInResponseGenerator>();
            builder.Services.AddTransient<SignInValidator>();
            builder.Services.AddTransient<IReturnUrlParser, WsFederationReturnUrlParser>();
            builder.Services.TryAddTransient<IRelyingPartyStore, NoRelyingPartyStore>();

            builder.Services.AddSingleton(
                resolver => resolver.GetRequiredService<IOptions<WsFederationOptions>>().Value);

            return builder;
        }

        public static IIdentityServerBuilder AddWsFederation(this IIdentityServerBuilder builder, Action<WsFederationOptions> setupAction)
        {
            builder.Services.Configure(setupAction);
            return builder.AddWsFederation();
        }

        public static IIdentityServerBuilder AddWsFederation(this IIdentityServerBuilder builder, IConfiguration configuration)
        {
            builder.Services.Configure<WsFederationOptions>(configuration);
            return builder.AddWsFederation();
        }

        public static IIdentityServerBuilder AddInMemoryRelyingParties(this IIdentityServerBuilder builder, IEnumerable<RelyingParty> relyingParties)
        {
            builder.Services.AddSingleton(relyingParties);
            builder.Services.AddSingleton<IRelyingPartyStore, InMemoryRelyingPartyStore>();

            return builder;
        }

        public static IIdentityServerBuilder AddRelyingPartyStore<T>(this IIdentityServerBuilder builder)
            where T : class, IRelyingPartyStore
        {
            builder.Services.AddTransient<IRelyingPartyStore, T>();

            return builder;
        }

        public static IIdentityServerBuilder AddRelyingPartyStoreCache<T>(this IIdentityServerBuilder builder)
            where T : class, IRelyingPartyStore
        {
            builder.Services.TryAddTransient(typeof(T));
            builder.Services.AddTransient<IRelyingPartyStore, CachingRelyingPartyStore<T>>();

            return builder;
        }
    }
}