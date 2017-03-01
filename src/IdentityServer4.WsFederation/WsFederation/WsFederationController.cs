// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.WsFederation.Validation;
using IdentityServer4.Configuration;
using IdentityServer4.Services;

namespace IdentityServer4.WsFederation
{
    public class WsFederationController : Controller
    {
        private readonly IClientSessionService _clientSessionService;
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
            IClientSessionService clientSessionService,
            ILogger<WsFederationController> logger)
        {
            _metadata = metadata;
            _signinValidator = signinValidator;
            _options = options;
            _generator = generator;
            _clientSessionService = clientSessionService;

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
                    return ProcessSignOutAsync(signout);
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
                var returnUrl = Url.Action("Index");
                returnUrl = returnUrl.AddQueryString(Request.QueryString.Value);

                var loginUrl = _options.UserInteraction.LoginUrl;
                var url = loginUrl.AddQueryString(_options.UserInteraction.LoginReturnUrlParameter, returnUrl);

                return Redirect(url);
            }
            else
            {
                // create protocol response
                var responseMessage = await _generator.GenerateResponseAsync(result);
                await _clientSessionService.AddClientIdAsync(result.Client.ClientId);
                
                return new SignInResult(responseMessage);
            }
        }

        private IActionResult ProcessSignOutAsync(SignOutRequestMessage signout)
        {
            return Redirect("~/connect/endsession");
        }
    }
}