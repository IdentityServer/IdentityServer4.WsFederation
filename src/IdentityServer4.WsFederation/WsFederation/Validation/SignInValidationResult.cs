
using IdentityServer4.Models;
using System.ComponentModel;
using System.IdentityModel.Services;
using System.Security.Claims;

namespace IdentityServer4.WsFederation.Validation
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SignInValidationResult
    {
        public bool IsError => !string.IsNullOrWhiteSpace(Error);
        public string Error { get; set; }
        public string ErrorMessage { get; set; }

        public SignInRequestMessage SignInRequestMessage { get; set; }
        
        public ClaimsPrincipal User { get; set; }
        public bool SignInRequired { get; set; }

        public Client Client { get; set; }
        public string ReplyUrl { get; set; }

        
    }
}