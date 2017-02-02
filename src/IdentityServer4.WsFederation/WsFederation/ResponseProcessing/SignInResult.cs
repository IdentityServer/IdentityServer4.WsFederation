using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Metadata;
using System.IdentityModel.Services;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IdentityServer4.WsFederation
{
    public class SignInResult : IActionResult
    {
        public SignInResponseMessage Message { get; set; }

        public SignInResult(SignInResponseMessage message)
        {
            Message = message;
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.ContentType = "text/html";
            return context.HttpContext.Response.WriteAsync(Message.WriteFormPost());
        }
    }
}
