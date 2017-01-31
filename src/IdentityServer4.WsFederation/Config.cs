using IdentityModel;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace IdentityServer4.WsFederation
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new[]
            {
                new IdentityResources.OpenId(),
                new IdentityResource("profile", new[] { JwtClaimTypes.Name, JwtClaimTypes.Email })
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new[]
            {
                new ApiResource("api1", "Some API 1"),
                new ApiResource("api2", "Some API 2")
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new[]
            {
                new Client
                {
                    ClientId = "client",
                    ClientSecrets = { new Secret("secret".Sha256()) },

                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = { "api1" }
                }
            };
        }
    }
}