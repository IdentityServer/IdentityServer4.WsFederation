// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
    /// <summary>
    /// A per-request authentication handler for the WsFederation.
    /// </summary>
    public class WsFederationHandler : RemoteAuthenticationHandler<WsFederationOptions>, IAuthenticationSignOutHandler
    {        
        private WsFederationConfiguration _configuration;

        /// <summary>
        /// Creates a new WsFederationAuthenticationHandler
        /// </summary>
        /// <param name="logger"></param>
        public WsFederationHandler(IOptionsMonitor<WsFederationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        /// <summary>
        /// Handles Challenge
        /// </summary>
        /// <returns></returns>
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            if (_configuration == null)
            {
                _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
            }

            string baseUri =
                    Request.Scheme +
                    Uri.SchemeDelimiter +
                    Request.Host +
                    Request.PathBase;

            string currentUri =
                baseUri +
                Request.Path +
                Request.QueryString;

            // Save the original challenge URI so we can redirect back to it when we're done.
            if (string.IsNullOrEmpty(properties.RedirectUri))
            {
                properties.RedirectUri = currentUri;
            }

            WsFederationMessage wsFederationMessage = new WsFederationMessage()
            {
                IssuerAddress = _configuration.TokenEndpoint ?? string.Empty,
                Wtrealm = Options.Wtrealm,
                Wctx = WsFederationDefaults.WctxKey + "=" + Uri.EscapeDataString(Options.StateDataFormat.Protect(properties)),
                Wa = WsFederationConstants.WsFederationActions.SignIn,
            };

            if (!string.IsNullOrWhiteSpace(Options.Wreply))
            {
                wsFederationMessage.Wreply = Options.Wreply;
            }

            var redirectContext = new RedirectContext(Context, Scheme, Options, properties)
            {
                ProtocolMessage = wsFederationMessage
            };
            await Options.Events.RedirectToIdentityProvider(redirectContext);

            if (!redirectContext.Handled)
            {
                string redirectUri = redirectContext.ProtocolMessage.CreateSignInUrl();
                if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
                {
                    Logger.LogWarning("The sign-in redirect URI is malformed: " + redirectUri);
                }
                Response.Redirect(redirectUri);
            }
        }

        /// <summary>
        /// Invoked to process incoming authentication messages.
        /// </summary>
        /// <returns></returns>
        protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            WsFederationMessage wsFederationMessage = null;

            // assumption: if the ContentType is "application/x-www-form-urlencoded" it should be safe to read as it is small.
            if (HttpMethods.IsPost(Request.Method)
              && !string.IsNullOrWhiteSpace(Request.ContentType)
              // May have media/type; charset=utf-8, allow partial match.
              && Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
              && Request.Body.CanRead)
            {
                var form = await Request.ReadFormAsync();
    
                wsFederationMessage = new WsFederationMessage(form.Select(pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value)));
            }

            if (wsFederationMessage == null || !wsFederationMessage.IsSignInMessage)
            {
                if (Options.SkipUnrecognizedRequests)
                {
                    // Not for us?
                    return HandleRequestResult.SkipHandler();
                }

                return HandleRequestResult.Fail("No message.");
            }
            
            try
            {
                // Retrieve our cached redirect uri
                string state = wsFederationMessage.Wctx;
                // WsFed allows for uninitiated logins, state may be missing.
                var properties = GetPropertiesFromWctx(state);

                var messageReceivedContext = new MessageReceivedContext(Context, Scheme, Options, properties)
                {
                    ProtocolMessage = wsFederationMessage
                };
                await Options.Events.MessageReceived(messageReceivedContext);
                if (messageReceivedContext.Result != null)
                {
                    return messageReceivedContext.Result;
                }

                if (wsFederationMessage.Wresult == null)
                {
                    Logger.LogWarning("Received a sign-in message without a WResult.");
                    return (HandleRequestResult)HandleRequestResult.NoResult();
                }

                string token = wsFederationMessage.GetToken();
                if (string.IsNullOrWhiteSpace(token))
                {
                    Logger.LogWarning("Received a sign-in message without a token.");
                    return (HandleRequestResult)HandleRequestResult.NoResult();
                }

                var securityTokenReceivedContext = new SecurityTokenReceivedContext(Context, Scheme, Options, properties)
                {
                    ProtocolMessage = wsFederationMessage
                };
                await Options.Events.SecurityTokenReceived(securityTokenReceivedContext);
                if (securityTokenReceivedContext.Result != null)
                {
                    return securityTokenReceivedContext.Result;
                }

                if (_configuration == null)
                {
                    _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
                }

                // Copy and augment to avoid cross request race conditions for updated configurations.
                var tvp = Options.TokenValidationParameters.Clone();
                IEnumerable<string> issuers = new[] { _configuration.Issuer };
                tvp.ValidIssuers = (tvp.ValidIssuers == null ? issuers : tvp.ValidIssuers.Concat(issuers));
                tvp.IssuerSigningKeys = (tvp.IssuerSigningKeys == null ? _configuration.SigningKeys : tvp.IssuerSigningKeys.Concat(_configuration.SigningKeys));

                ClaimsPrincipal principal = null;
                SecurityToken parsedToken = null;
                foreach (var validator in Options.SecurityTokenHandlers)
                {
                    if (validator.CanReadToken(token))
                    {
                        principal = validator.ValidateToken(token, tvp, out parsedToken);
                        break;
                    }
                }

                if (principal == null)
                {
                    throw new SecurityTokenException("no validator found");
                }

                if (Options.UseTokenLifetime)
                {
                    // Override any session persistence to match the token lifetime.
                    DateTime issued = parsedToken.ValidFrom;
                    if (issued != DateTime.MinValue)
                    {
                        properties.IssuedUtc = issued.ToUniversalTime();
                    }
                    DateTime expires = parsedToken.ValidTo;
                    if (expires != DateTime.MinValue)
                    {
                        properties.ExpiresUtc = expires.ToUniversalTime();
                    }
                    properties.AllowRefresh = false;
                }

                var securityTokenValidatedContext = new SecurityTokenValidatedContext(Context, Scheme, Options, principal, properties)
                {
                    ProtocolMessage = wsFederationMessage,
                };

                await Options.Events.SecurityTokenValidated(securityTokenValidatedContext);
                if (securityTokenValidatedContext.Result != null)
                {
                    return securityTokenValidatedContext.Result;
                }

                // Flow possible changes
                principal = securityTokenValidatedContext.Principal;
                properties = securityTokenValidatedContext.Properties;

                return HandleRequestResult.Success(new AuthenticationTicket(principal, properties, Scheme.Name));
            }
            catch (Exception exception)
            {
                Logger.LogError("Exception occurred while processing message: ", exception);

                // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the notification.
                if (Options.RefreshOnIssuerKeyNotFound && exception.GetType().Equals(typeof(SecurityTokenSignatureKeyNotFoundException)))
                {
                    Options.ConfigurationManager.RequestRefresh();
                }

                var authenticationFailedContext = new AuthenticationFailedContext(Context, Scheme, Options)
                {
                    ProtocolMessage = wsFederationMessage,
                    Exception = exception
                };
                await Options.Events.AuthenticationFailed(authenticationFailedContext);
                if (authenticationFailedContext.Result != null)
                {
                    return authenticationFailedContext.Result;
                }

                throw;
            }
        }
        
        private AuthenticationProperties GetPropertiesFromWctx(string state)
        {
            AuthenticationProperties properties = null;
            if (!string.IsNullOrEmpty(state))
            {
                var pairs = ParseDelimited(state);
                List<string> values;
                if (pairs.TryGetValue(WsFederationDefaults.WctxKey, out values) && values.Count > 0)
                {
                    string value = values.First();
                    properties = Options.StateDataFormat.Unprotect(value);
                }
            }
            return properties;
        }

        private static IDictionary<string, List<string>> ParseDelimited(string text)
        {
            char[] delimiters = new[] { '&', ';' };
            var accumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            int textLength = text.Length;
            int equalIndex = text.IndexOf('=');
            if (equalIndex == -1)
            {
                equalIndex = textLength;
            }
            int scanIndex = 0;
            while (scanIndex < textLength)
            {
                int delimiterIndex = text.IndexOfAny(delimiters, scanIndex);
                if (delimiterIndex == -1)
                {
                    delimiterIndex = textLength;
                }
                if (equalIndex < delimiterIndex)
                {
                    while (scanIndex != equalIndex && char.IsWhiteSpace(text[scanIndex]))
                    {
                        ++scanIndex;
                    }
                    string name = text.Substring(scanIndex, equalIndex - scanIndex);
                    string value = text.Substring(equalIndex + 1, delimiterIndex - equalIndex - 1);

                    name = Uri.UnescapeDataString(name.Replace('+', ' '));
                    value = Uri.UnescapeDataString(value.Replace('+', ' '));

                    List<string> existing;
                    if (!accumulator.TryGetValue(name, out existing))
                    {
                        accumulator.Add(name, new List<string>(1) { value });
                    }
                    else
                    {
                        existing.Add(value);
                    }

                    equalIndex = text.IndexOf('=', delimiterIndex);
                    if (equalIndex == -1)
                    {
                        equalIndex = textLength;
                    }
                }
                scanIndex = delimiterIndex + 1;
            }
            return accumulator;
        }

        /// <summary>
        /// Handles Signout
        /// </summary>
        /// <returns></returns>
        public async virtual Task SignOutAsync(AuthenticationProperties properties)
        {
            if (_configuration == null)
            {
                _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
            }

            var wsFederationMessage = new WsFederationMessage()
            {
                IssuerAddress = _configuration.TokenEndpoint ?? string.Empty,
                Wtrealm = Options.Wtrealm,
                Wa = WsFederationConstants.WsFederationActions.SignOut,
            };

            // Set Wreply in order:
            // 1. properties.Redirect
            // 2. Options.SignOutWreply
            // 3. Options.Wreply
            if (properties != null && !string.IsNullOrEmpty(properties.RedirectUri))
            {
                wsFederationMessage.Wreply = properties.RedirectUri;
            }
            else if (!string.IsNullOrWhiteSpace(Options.SignOutWreply))
            {
                wsFederationMessage.Wreply = Options.SignOutWreply;
            }
            else if (!string.IsNullOrWhiteSpace(Options.Wreply))
            {
                wsFederationMessage.Wreply = Options.Wreply;
            }

            var redirectContext = new RedirectContext(Context, Scheme, Options, properties)
            {
                ProtocolMessage = wsFederationMessage
            };
            await Options.Events.RedirectToIdentityProvider(redirectContext);

            if (!redirectContext.Handled)
            {
                string redirectUri = redirectContext.ProtocolMessage.CreateSignOutUrl();
                if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
                {
                    Logger.LogWarning("The sign-out redirect URI is malformed: " + redirectUri);
                }
                Response.Redirect(redirectUri);
            }
        }
    }
}