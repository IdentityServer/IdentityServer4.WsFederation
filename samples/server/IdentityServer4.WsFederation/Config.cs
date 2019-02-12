using IdentityModel;
using IdentityServer4.Models;
using System.Collections.Generic;
using static IdentityServer4.IdentityServerConstants;

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
                    ClientId = "urn:owinrp",
                    ProtocolType = ProtocolTypes.WsFederation,

                    RedirectUris = { "http://localhost:10313/" },
                    FrontChannelLogoutUri = "http://localhost:10313/home/signoutcleanup",
                    IdentityTokenLifetime = 36000,

                    AllowedScopes = { "openid", "profile" }
                },
                new Client
                {
                    ClientId = "urn:aspnetcorerp",
                    ProtocolType = ProtocolTypes.WsFederation,

                    RedirectUris = { "http://localhost:10314/" },
                    FrontChannelLogoutUri = "http://localhost:10314/account/signoutcleanup",
                    IdentityTokenLifetime = 36000,

                    AllowedScopes = { "openid", "profile" }
                }
            };
        }
    }
}