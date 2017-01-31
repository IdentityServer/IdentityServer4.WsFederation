using IdentityServer4.Services;
using IdentityServer4.WsFederation;
using IdentityServer4.WsFederation.Validation;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WsFederationBuilderExtensions
    {
        public static IIdentityServerBuilder AddWsFederation(this IIdentityServerBuilder builder)
        {
            builder.Services.AddTransient<MetadataResponseGenerator>();
            builder.Services.AddTransient<SignInValidator>();
            builder.Services.AddTransient<IReturnUrlParser, WsFederationReturnUrlParser>();

            return builder;
        }
    }
}
