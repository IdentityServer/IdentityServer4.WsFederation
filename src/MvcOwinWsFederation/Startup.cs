using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.WsFederation;
using Owin;

[assembly: OwinStartup(typeof(MvcOwinWsFederation.Startup))]

namespace MvcOwinWsFederation
{
    public class Startup
    {
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

                    SignInAsAuthenticationType = "Cookies",
                    SignOutWreply = "http://localhost:10313/"
            });
        }
    }
}
