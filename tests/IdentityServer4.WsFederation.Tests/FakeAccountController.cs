using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServer4.WsFederation.Tests
{
    public class FakeAccountController : Controller
    {
        [HttpGet]
        [Route("account/login")]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var allClaims = new Claim[] {
                new Claim(JwtClaimTypes.Subject, "testSub"),
                new Claim(JwtClaimTypes.Name, "testName"),
            };
            var identity = new ClaimsIdentity(allClaims, "Fake IdP", JwtClaimTypes.Name, JwtClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("idsrv", principal);
            if(Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return View();
        }
    }
}