using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.WsFederation.Validation;
using IdentityServer4.Configuration;

namespace IdentityServer4.WsFederation
{
    public class WsFederationController : Controller
    {
        private readonly ILogger<WsFederationController> _logger;
        private readonly MetadataResponseGenerator _metadata;
        private readonly IdentityServerOptions _options;
        private readonly SignInValidator _signinValidator;

        public WsFederationController(
            MetadataResponseGenerator metadata, 
            SignInValidator signinValidator, 
            IdentityServerOptions options,
            ILogger<WsFederationController> logger)
        {
            _metadata = metadata;
            _signinValidator = signinValidator;
            _options = options;

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
            
            var url = Url.Action("Index", "WsFederation", null, Request.Scheme, Request.Host.Value) + Request.QueryString;
            _logger.LogDebug("Start WS-Federation request: {url}", url);

            WSFederationMessage message;
            var user = await HttpContext.GetIdentityServerUserAsync();

            if (WSFederationMessage.TryCreateFromUri(new Uri(url), out message))
            {
                var signin = message as SignInRequestMessage;
                if (signin != null)
                {
                    return await ProcessSignInAsync(signin, user);
                }

                var signout = message as SignOutRequestMessage;
                if (signout != null)
                {
                    return await ProcessSignOutAsync(signout);
                }
            }

            return BadRequest("Invalid WS-Federation request");
        }

        
        private async Task<IActionResult> ProcessSignInAsync(SignInRequestMessage signin, ClaimsPrincipal user)
        {
            if (user != null)
            {
                _logger.LogDebug("User in WS-Federation signin request: {subjectId}", user.GetSubjectId());
            }
            else
            {
                _logger.LogDebug("No user present in WS-Federation signin request");
            }

            // validate request
            var result = await _signinValidator.ValidateAsync(signin, user);

            if (result.IsError)
            {
                throw new Exception(result.Error);
            }

            if (result.SignInRequired)
            {
                //var returnUrl = Url.Action("Index", "WsFederation", null, Request.Scheme, Request.Host.Value);
                //returnUrl = returnUrl.AddQueryString(_request.Raw.ToQueryString());

                var returnUrl = Url.Action("Index");
                returnUrl = returnUrl.AddQueryString(Request.QueryString.Value);
                var loginUrl = _options.UserInteraction.LoginUrl;
                var url = loginUrl.AddQueryString(_options.UserInteraction.LoginReturnUrlParameter, returnUrl);

                return Redirect(url);
            }
            else
            {
                return null;
            }
            
            //var result = await _validator.ValidateAsync(msg, User as ClaimsPrincipal);

            //if (result.IsSignInRequired)
            //{
            //    //Logger.Info("Redirecting to login page");
            //    return RedirectToLogin(result);
            //}
            //if (result.IsError)
            //{
            //    //Logger.Error(result.Error);
            //    //await _events.RaiseFailureWsFederationEndpointEventAsync(
            //    //    WsFederationEventConstants.Operations.SignIn,
            //    //    result.RelyingParty.Realm,
            //    //    result.Subject,
            //    //    Request.RequestUri.AbsoluteUri,
            //    //    result.Error);

            //    return BadRequest(result.Error);
            //}

            //var responseMessage = await _signInResponseGenerator.GenerateResponseAsync(result);
            //await _cookies.AddValueAsync(WsFederationPluginOptions.CookieName, result.ReplyUrl);

            //await _events.RaiseSuccessfulWsFederationEndpointEventAsync(
            //        WsFederationEventConstants.Operations.SignIn,
            //        result.RelyingParty.Realm,
            //        result.Subject,
            //        Request.RequestUri.AbsoluteUri);

            //return new SignInResult(responseMessage);
        }


        private Task<IActionResult> ProcessSignOutAsync(SignOutRequestMessage signout)
        {
            throw new NotImplementedException();
        }

    }
}
