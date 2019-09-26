// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Services;
using System;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Extensions;
using Microsoft.Extensions.Logging;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Http;
using System.Net;
using Microsoft.IdentityModel.Protocols.WsFederation;

namespace IdentityServer4.WsFederation
{
    public class WsFederationReturnUrlParser : IReturnUrlParser
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<WsFederationReturnUrlParser> _logger;
        private readonly SignInValidator _signinValidator;
        private readonly IUserSession _userSession;

        public WsFederationReturnUrlParser(
            IUserSession userSession,
            IHttpContextAccessor contextAccessor,
            SignInValidator signinValidator,
            ILogger<WsFederationReturnUrlParser> logger)
        {
            _contextAccessor = contextAccessor;
            _signinValidator = signinValidator;
            _userSession = userSession;
            _logger = logger;
        }

        public bool IsValidReturnUrl(string returnUrl)
        {
            if (returnUrl.IsLocalUrl())
            {
                var message = GetSignInRequestMessage(returnUrl);
                if (message != null) return true;

                _logger.LogTrace("not a valid WS-Federation return URL");
                return false;                
            }

            return false;
        }

        public async Task<AuthorizationRequest> ParseAsync(string returnUrl)
        {
            var user = await _userSession.GetUserAsync();

            var signInMessage = GetSignInRequestMessage(returnUrl);
            if (signInMessage == null) return null;

            // call validator
            var result = await _signinValidator.ValidateAsync(signInMessage, user);
            if (result.IsError) return null;

            // populate request
            var request = new AuthorizationRequest()
            {
                ClientId = result.Client.ClientId,
                IdP = result.WsFederationMessage.Wtrealm,
                RedirectUri = result.WsFederationMessage.Wreply
            };

            foreach (var item in result.WsFederationMessage.Parameters)
            {
                request.Parameters.Add(item.Key, item.Value);
            }

            return request;
        }

        private WsFederationMessage GetSignInRequestMessage(string returnUrl)
        {
            var decoded = WebUtility.UrlDecode(returnUrl);
            WsFederationMessage message = WsFederationMessage.FromQueryString(decoded);
            if (message.IsSignInMessage)
                return message;
            return null;
        }
    }
}