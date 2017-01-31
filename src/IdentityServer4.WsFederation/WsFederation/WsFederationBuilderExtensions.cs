using IdentityServer4.WsFederation;
using IdentityServer4.WsFederation.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WsFederationBuilderExtensions
    {
        public static IIdentityServerBuilder AddWsFederation(this IIdentityServerBuilder builder)
        {
            builder.Services.AddTransient<MetadataResponseGenerator>();
            builder.Services.AddTransient<SignInValidator>();

            return builder;
        }
    }
}
