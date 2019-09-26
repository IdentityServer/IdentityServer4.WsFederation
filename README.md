# IdentityServer4.WsFederation
**Sample** for implementing WS-Federation IdP support for IdentityServer4 with .NET core.

## Overview
IdentityServer4 is designed to be extensible with custom protocol endpoints.
This repo shows a simple implementation of WS-Federation IdP services.
This is useful for connecting SharePoint or older ASP.NET relying parties to IdentityServer.

**This is not supposed to be a generic WS-Federation implementation, but is rather a sample that you can use 
as a starting point to build your own WS-Federation support (or even for inspiration for integrating other custom protocols, which 
are not natively supported by IdentityServer4).**

The following is a brief description of some technical points of interest. Feel free to amend this document if more details are needed.

## .NET Support
The underlying WS-Federation classes use .NET Core.

## WS-Federation endpoint
The WS-Federation endpoint (metadata, sign-in and out) is implemented via an MVC controller (~/wsfederation).
This controller handles the WS-Federation protocol requests and redirects the user to the login page if needed.

The login page will then use the normal return URL mechanism to redirect back to the WS-Federation endpoint
to create the protocol response.

## Response generation
The `SignInResponseGenerator` class does the heavy lifting of creating the contents of the WS-Federation response:

* it calls the IdentityServer profile service to retrieve the configured claims for the relying party
* it tries to map the standard claim types to WS-* style claim types
* it creates the SAML 1.1/2.0 token
* it creates the RSTR (request security token response)

The outcome of these operations is a `SignInResponseMessage` object which then gets turned into a WS-Federation response and sent back to the relying party.

## Configuration
For most parts, the WS-Federation endpoint can use the standard IdentityServer4 client configuration for relying parties.
But there are also options available for setting WS-Federation specific options.

### Defaults
You can configure global defaults in the `WsFederationOptions` class, e.g.:

* default token type (SAML 1.1 or SAML 2.0)
* default hashing and digest algorithms
* default SAML name identifier format
* default mappings from "short" claim types to WS-* claim types

### Relying party configuration
The following client settings are used by the WS-Federation endpoint:

```csharp
public static IEnumerable<Client> GetClients()
{
    return new[]
    {
        new Client
        {
            // realm identifier
            ClientId = "urn:owinrp",
            
            // must be set to WS-Federation
            ProtocolType = ProtocolTypes.WsFederation,

            // reply URL
            RedirectUris = { "http://localhost:10313/" },
            
            // signout cleanup url
            LogoutUri = "http://localhost:10313/home/signoutcleanup",
            
            // lifetime of SAML token
            IdentityTokenLifetime = 36000,

            // identity scopes - the associated claims will be used to call the profile service
            AllowedScopes = { "openid", "profile" }
        }
    };
}
```

### WS-Federation specific relying party settings
If you want to deviate from the global defaults (e.g. set a different token type or claim mapping) for a specific
relying party, you can define a `RelyingParty` object that uses the same realm name as the client ID used above.

This sample contains an in-memory relying party store that you can use to make these relying party specific settings
available to the WS-Federation engine (using the `AddInMemoryRelyingParty` extension method).
Otherwise, if you want to use your own store, you will need an implementation of `IRelyingPartyStore`.

### Configuring IdentityServer
This repo contains an extension method for the IdentityServer builder object to register all the necessary services in DI, e.g.:

```csharp
services.AddIdentityServer()
    .AddSigningCredential(cert)
    .AddInMemoryIdentityResources(Config.GetIdentityResources())
    .AddInMemoryApiResources(Config.GetApiResources())
    .AddInMemoryClients(Config.GetClients())
    .AddTestUsers(TestUsers.Users)
    .AddWsFederation()
    .AddInMemoryRelyingParties(Config.GetRelyingParties());
```

## Connecting a relying party to the WS-Federation endpoint

### Using Katana
Use the Katana WS-Federation middleware to point to the WS-Federation endpoint, e.g.:

```csharp
public void Configuration(IAppBuilder app)
{
    app.UseCookieAuthentication(new CookieAuthenticationOptions
    {
        AuthenticationType = "Cookies"
    });

    app.UseWsFederationAuthentication(new WsFederationAuthenticationOptions
    {
        MetadataAddress = "http://localhost:5000/wsfederation",
        Wtrealm = "urn:owinrp",

        SignInAsAuthenticationType = "Cookies"
    });
}
```

### SharePoint

see https://www.scottbrady91.com/Identity-Server/IdentityServer-4-SharePoint-Integration-using-WS-Federation
