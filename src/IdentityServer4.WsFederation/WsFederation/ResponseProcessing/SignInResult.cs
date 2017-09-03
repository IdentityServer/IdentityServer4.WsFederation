// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation
{
    public class SignInResult : IActionResult
    {
        public WsFederationMessage Message { get; set; }

        public SignInResult(WsFederationMessage message)
        {
            Message = message;
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.ContentType = "text/html";
            return context.HttpContext.Response.WriteAsync(Message.BuildFormPost());
        }
    }
}