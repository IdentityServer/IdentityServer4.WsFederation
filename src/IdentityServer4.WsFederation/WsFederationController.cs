// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.WsFederation.Validation;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using Microsoft.IdentityModel.Protocols.WsFederation;

namespace IdentityServer4.WsFederation
{
    public class WsFederationController : Controller
    {
        private readonly IUserSession _userSession;
        private readonly SignInResponseGenerator _generator;
        private readonly ILogger<WsFederationController> _logger;
        private readonly MetadataResponseGenerator _metadata;
        private readonly IdentityServerOptions _options;
        private readonly SignInValidator _signinValidator;

        public WsFederationController(
            MetadataResponseGenerator metadata, 
            SignInValidator signinValidator, 
            IdentityServerOptions options,
            SignInResponseGenerator generator,
            IUserSession userSession,
            ILogger<WsFederationController> logger)
        {
            _metadata = metadata;
            _signinValidator = signinValidator;
            _options = options;
            _generator = generator;
            _userSession = userSession;
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

            var user = await _userSession.GetUserAsync();
            WsFederationMessage message = WsFederationMessage.FromUri(new Uri(url));
            var isSignin = message.IsSignInMessage;
            if (isSignin)
            {
                return await ProcessSignInAsync(message, user);
            }
            var isSignout = message.IsSignOutMessage;
            if (isSignout)
            {
                return ProcessSignOutAsync(message);
            }

            return BadRequest("Invalid WS-Federation request");
        }

        
        private async Task<IActionResult> ProcessSignInAsync(WsFederationMessage signin, ClaimsPrincipal user)
        {
            if (user.Identity.IsAuthenticated)
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
                var returnUrl = Url.Action("Index");
                returnUrl = returnUrl.AddQueryString(Request.QueryString.Value);

                var loginUrl = Request.PathBase + _options.UserInteraction.LoginUrl;
                var url = loginUrl.AddQueryString(_options.UserInteraction.LoginReturnUrlParameter, returnUrl);

                return Redirect(url);
            }
            else
            {
                // create protocol response
                var responseMessage = await _generator.GenerateResponseAsync(result);
                await _userSession.AddClientIdAsync(result.Client.ClientId);
                
                return new SignInResult(responseMessage);
            }
        }

        private IActionResult ProcessSignOutAsync(WsFederationMessage signout)
        {
            return Redirect("~/connect/endsession");
        }
    }
}