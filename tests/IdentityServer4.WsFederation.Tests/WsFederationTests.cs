using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using IdentityServer4;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using IdentityServer4.WsFederation;
using IdentityServer4.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using IdentityServer4.WsFederation.Validation;
using IdentityServer4.Configuration;
using IdentityServer4.WsFederation.Stores;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Net;
using System.Collections.Generic;
using Microsoft.Net.Http.Headers;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.IdentityModel.Tokens.Saml;

namespace IdentityServer4.WsFederation.Tests
{
    public class WsFederationTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        public WsFederationTests()
        {
            var builder = new WebHostBuilder()
                 .ConfigureServices(InitializeServices)
                 .Configure(app =>
                 {
                     app.UseIdentityServer();
                     app.UseMvc(routes =>
                        routes.MapRoute(
                            "default",
                            "{controller}/{action=index}/{id?}"
                        )
                     );
                 });
                // .UseStartup<Startup>();
            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        protected virtual void InitializeServices(IServiceCollection services)
        {
            var startupAssembly = typeof(Startup).GetTypeInfo().Assembly;
            var wsFedController = typeof(WsFederationController).GetTypeInfo().Assembly;
            var accountController = typeof(FakeAccountController).GetTypeInfo().Assembly;

            // Inject a custom application part manager. Overrides AddMvcCore() because that uses TryAdd().
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new AssemblyPart(startupAssembly));
            manager.ApplicationParts.Add(new AssemblyPart(wsFedController));
            manager.ApplicationParts.Add(new AssemblyPart(accountController));

            manager.FeatureProviders.Add(new ControllerFeatureProvider());
            manager.FeatureProviders.Add(new ViewComponentFeatureProvider());

            services.AddSingleton(manager);
            services.TryAddSingleton<IKeyMaterialService>(
                new DefaultKeyMaterialService(
                    new IValidationKeysStore[] { }, 
                    new DefaultSigningCredentialsStore(TestCert.LoadSigningCredentials()))); 
            // TestLogger.Create<DefaultTokenCreationService>()));

            services.AddIdentityServer()
                .AddSigningCredential(TestCert.LoadSigningCredentials())
                .AddInMemoryClients(Config.GetClients())
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryRelyingParties(Config.GetRelyingParties())
                .AddWsFederation();
            services.TryAddTransient<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMvc();
        }

        [Fact]
        public async Task WsFederation_metadata_success()
        {
            var response = await _client.GetAsync("/wsfederation");
            var message = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(message);
            Assert.True(message.StartsWith("<EntityDescriptor entityID=\"http://localhost\""));
        }

        [Fact]
        public async Task WsFederation_signin_and_redirect_to_login_page_Success()
        {
            var wsMessage = new WsFederationMessage
            {
                IssuerAddress = "/wsfederation",
                Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
            };
            var singInUrl = wsMessage.CreateSignInUrl();
            var response = await _client.GetAsync(singInUrl);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var expectedLocation = "/Account/Login?ReturnUrl=%2Fwsfederation%3Fwtrealm%3Durn%253Aowinrp%26wreply%3Dhttp%253A%252F%252Flocalhost%253A10313%252F%26wa%3Dwsignin1.0";
            Assert.Equal(expectedLocation, response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task WsFederation_login_and_return_assertion_success()
        {
            var loginUrl = "/account/login?returnUrl=%2Fwsfederation%3Fwtrealm%3Durn%253Aowinrp%26wreply%3Dhttp%253A%252F%252Flocalhost%253A10313%252F%26wa%3Dwsignin1.0";
            var response = await _client.GetAsync(loginUrl);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var expectedLocation = "/wsfederation?wtrealm=urn%3Aowinrp&wreply=http%3A%2F%2Flocalhost%3A10313%2F&wa=wsignin1.0";
            Assert.Equal(expectedLocation, response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task WsFederation_login_return_assertion_success()
        {
            var loginUrl = "/account/login?returnUrl=%2Fwsfederation%3Fwtrealm%3Durn%253Aowinrp%26wreply%3Dhttp%253A%252F%252Flocalhost%253A10313%252F%26wa%3Dwsignin1.0";
            var response = await _client.GetAsync(loginUrl);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var wsEndpointUrl = "/wsfederation?wtrealm=urn%3Aowinrp&wreply=http%3A%2F%2Flocalhost%3A10313%2F&wa=wsignin1.0";
            Assert.Equal(wsEndpointUrl, response.Headers.Location.OriginalString);
            var request = GetRequest(wsEndpointUrl, response);
            var wsResponse = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, wsResponse.StatusCode);
            var contentAsText = await wsResponse.Content.ReadAsStringAsync();
            Assert.Contains("action=\"http://localhost:10313/\"", contentAsText);
            var wreturn = ExtractInBetween(contentAsText, "wresult\" value=\"", "\"");
            Assert.False(wreturn.StartsWith("%EF%BB%BF")); //don't start with BOM (Byte Order Mark)
            var wsMessage = new WsFederationMessage 
            {
                Wresult = WebUtility.HtmlDecode(wreturn),
            };
            var tokenString = wsMessage.GetToken();
            var handler = new SamlSecurityTokenHandler();
            var canReadToken = handler.CanReadToken(tokenString);
            Assert.True(canReadToken);
        }

        private string ExtractInBetween(string source, string startStr, string endStr)
        {
            var startIndex = source.IndexOf(startStr) + startStr.Length;
            var length = source.IndexOf(endStr, startIndex) - startIndex;
            var result = source.Substring(startIndex, length);
            return result;
        }

        private HttpRequestMessage GetRequest(string path, HttpResponseMessage response)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            IEnumerable<string> values;
            if (response.Headers.TryGetValues("Set-Cookie", out values))
            {
                var setCookieHeaderValues = SetCookieHeaderValue.ParseList(values.ToList());
                var cookiesValues = setCookieHeaderValues.Select(c => new CookieHeaderValue(c.Name, c.Value).ToString());
                var cookieHeaderValue = string.Join("; ", cookiesValues);
                request.Headers.Add("Cookie", cookieHeaderValue);
            }
            return request;
        }
    }
}
