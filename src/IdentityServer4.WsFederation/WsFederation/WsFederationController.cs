using IdentityServer4.Services;
using LiebherrPoc.WsFederation.IdentityServer4.WsFederation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Services;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation
{
    public class WsFederationController : Controller
    {
        private readonly ILogger<WsFederationController> _logger;
        private readonly MetadataResponseGenerator _metadata;

        public WsFederationController(MetadataResponseGenerator metadata, ILogger<WsFederationController> logger)
        {
            _metadata = metadata;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // GET + no parameters = metadata request
            if (!Request.QueryString.HasValue)
            {
                _logger.LogDebug("Start WS-Federation metadata request");

                var entity = await _metadata.GenerateAsync(Url.Action("Index", "WsFederation", null, Request.Scheme, Request.Host.Value));
                return new MetadataResult(entity);
            }

            _logger.LogDebug("Start WS-Federation request: {url}", Request.Path.Value);
            
            WSFederationMessage message;
            Uri publicRequestUri = new Uri(Request.Path);

            if (WSFederationMessage.TryCreateFromUri(publicRequestUri, out message))
            {
                var signin = message as SignInRequestMessage;
                if (signin != null)
                {
                    return await ProcessSignInAsync(signin);
                }

                var signout = message as SignOutRequestMessage;
                if (signout != null)
                {
                    return await ProcessSignOutAsync(signout);
                }
            }

            return BadRequest("Invalid WS-Federation request");
        }

        private Task<IActionResult> ProcessSignOutAsync(SignOutRequestMessage signout)
        {
            throw new NotImplementedException();
        }

        private Task<IActionResult> ProcessSignInAsync(SignInRequestMessage signin)
        {
            throw new NotImplementedException();
        }
    }
}
