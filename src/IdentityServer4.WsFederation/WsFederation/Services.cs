using System.Threading.Tasks;

namespace IdentityServer4.WsFederation
{
    public interface IRelyingPartyStore
    {
        Task<RelyingParty> FindRelyingPartyByRealm(string realm);
    }
}