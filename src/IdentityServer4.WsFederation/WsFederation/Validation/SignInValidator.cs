
using IdentityServer4.Stores;
using System;
using System.ComponentModel;
using System.IdentityModel.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Validation
{
    public class SignInValidator
    {
        private readonly IClientStore _clients;

        //private readonly static ILog Logger = LogProvider.GetCurrentClassLogger();

        //private readonly IRelyingPartyService _relyingParties;
        //private readonly ICustomWsFederationRequestValidator _customValidator;

        //public SignInValidator(IRelyingPartyService relyingParties, ICustomWsFederationRequestValidator customValidator)
        //{
        //    _relyingParties = relyingParties;
        //    _customValidator = customValidator;
        //}

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
                LogError("Relying party not found: " + message.Realm, result);

                return new SignInValidationResult
                {
                    Error = "invalid_relying_party"
                };
            }

            result.Client = client;

            if (user == null ||
                user.Identity.IsAuthenticated == false)
            {
                result.SignInRequired = true;
                return result;
            }

            //result.ReplyUrl = rp.ReplyUrl;
            //result.RelyingParty = rp;
            //result.SignInRequestMessage = message;
            //result.Subject = subject;

            //Logger.Debug("Calling into custom validator: " + _customValidator.GetType().FullName);
            //var customResult = await _customValidator.ValidateSignInRequestAsync(result);
            //if (customResult.IsError)
            //{
            //    LogError("Error in custom validation: " + customResult.Error, result);
            //    return new SignInValidationResult
            //        {
            //            IsError = true,
            //            Error = customResult.Error,
            //            ErrorMessage = customResult.ErrorMessage,
            //        };
            //}

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