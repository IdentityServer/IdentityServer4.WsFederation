
using IdentityServer4.Models;
using System.ComponentModel;
using System.IdentityModel.Services;

namespace IdentityServer4.WsFederation.Validation
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SignInValidationResult
    {
        public SignInRequestMessage SignInRequestMessage { get; set; }

        public bool IsError => !string.IsNullOrWhiteSpace(Error);
        public string Error { get; set; }
        public string ErrorMessage { get; set; }

        public bool SignInRequired { get; set; }
        public Client Client { get; set; }

        //public RelyingParty RelyingParty { get; set; }
        //public string ReplyUrl { get; set; }
        //public string HomeRealm { get; set; }
        //public string Federation { get; set; }
        //public ClaimsPrincipal Subject { get; set; }
    }
}