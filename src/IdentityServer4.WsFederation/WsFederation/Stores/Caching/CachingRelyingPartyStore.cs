using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.WsFederation.Stores
{
    /// <summary>
    /// Cache decorator for IRelyingPartyStore
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IdentityServer4.WsFederation.Stores.IRelyingPartyStore"/>
    public class CachingRelyingPartyStore<T> : IRelyingPartyStore
        where T : IRelyingPartyStore
    {
        private readonly IdentityServerOptions _options;
        private readonly ICache<RelyingParty> _cache;
        private readonly IRelyingPartyStore _inner;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingRelyingPartyStore{T}"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="logger">The logger.</param>
        public CachingRelyingPartyStore(IdentityServerOptions options, T inner, ICache<RelyingParty> cache, ILogger<CachingRelyingPartyStore<T>> logger)
        {
            _options = options;
            _inner = inner;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Finds a relying party by realm
        /// </summary>
        /// <param name="realm">The realm</param>
        /// <returns>
        /// The relying party
        /// </returns>
        public async Task<RelyingParty> FindRelyingPartyByRealm(string realm)
        {
            var relyingParty = await _cache.GetAsync(realm,
                _options.Caching.ClientStoreExpiration,
                () => _inner.FindRelyingPartyByRealm(realm),
                _logger);

            return relyingParty;
        }
    }
}
