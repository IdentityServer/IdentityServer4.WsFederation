using IdentityServer4.Stores;
using System.IdentityModel.Services;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static IdentityServer4.IdentityServerConstants;

namespace IdentityServer4.WsFederation.Validation
{
    public class SignInValidator
    {
        private readonly IClientStore _clients;

        public SignInValidator(IClientStore clients)
        {
            _clients = clients;
        }

        public async Task<SignInValidationResult> ValidateAsync(SignInRequestMessage message, ClaimsPrincipal user)
        {
            //Logger.Info("Start WS-Federation signin request validation");
            var result = new SignInValidationResult
            {
                SignInRequestMessage = message
            };
            
            // check client
            var client = await _clients.FindEnabledClientByIdAsync(message.Realm);

            if (client == null)
            {
                LogError("Client not found: " + message.Realm, result);

                return new SignInValidationResult
                {
                    Error = "invalid_relying_party"
                };
            }
            if (client.ProtocolType != ProtocolTypes.WsFederation)
            {
                LogError("Client is not configured for WS-Federation", result);

                return new SignInValidationResult
                {
                    Error = "invalid_relying_party"
                };
            }

            result.Client = client;
            result.ReplyUrl = client.RedirectUris.First();

            if (user == null ||
                user.Identity.IsAuthenticated == false)
            {
                result.SignInRequired = true;
            }

            result.User = user;
            
            LogSuccess(result);
            return result;
        }

        private void LogSuccess(SignInValidationResult result)
        {
            //var log = new SignInValidationLog(result);
            //Logger.InfoFormat("End WS-Federation signin request validation\n{0}", log.ToString());
        }

        private void LogError(string message, SignInValidationResult result)
        {
            //var log = new SignInValidationLog(result);
            //Logger.ErrorFormat("{0}\n{1}", message, log.ToString());
        }
    }
}