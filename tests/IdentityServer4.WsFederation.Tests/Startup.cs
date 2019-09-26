using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IdentityServer4.WsFederation;
using System.Reflection;

namespace IdentityServer4.WsFederation.Tests
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentityServer()
                .AddWsFederation();
            services.AddMvc();
                // .AddApplicationPart(Assembly.Load(new AssemblyName("IdentityServer4.WsFederation")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)//, IHostingEnvironment env, ILoggerFactory logger)
        {
            // app.UseIdentityServer();
            app.UseMvc();
        }
    }
}