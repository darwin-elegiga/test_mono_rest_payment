using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.DocSys.Parsing.Common
{
    public class Edi835ConfigurationCache : LocalMemoryCache<string, string>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public Edi835ConfigurationCache(IServiceScopeFactory serviceScopeFactory)
            : base(new LocalMemoryCacheOptions
            {
                UseSlidingExpiration = true,
                ItemExpirationTime = TimeSpan.FromHours(12),
                MinimumExpirationScanFrequency = TimeSpan.FromMinutes(5)
            })
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// Checks if tpa exists in the Edi835Configuration table to determine if Edi835 is needed
        /// </summary>
        /// <param name="tpa">The client code</param>
        /// <param name="origin">The client's origin</param>
        /// <remarks>If the origin is redcard "RC" then it just returns true</remarks>
        /// <returns>true if 835 is needed, false if it isn't</returns>
        public async Task<bool> IsEdi835Needed(string tpa, string origin)
        {
            if ("RC".Equals(origin, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            // if value exists in cache
            if (this.ContainsKey(tpa))
            {
                return true;
            }
            else
            {
                // Key not in cache, update cache and try again
                await UpdateCache();
                return this.ContainsKey(tpa);
            }
        }

        /// <summary>
        /// This method checks the cache for the clientCode and returns the parser
        /// </summary>
        /// <param name="clientCode">The client code</param>
        /// <returns>A string containing the parser name, empty string if clientCode not found</returns>
        public async Task<string> GetParserFromClientCode(string clientCode)
        {
            var cacheValue = string.Empty;

            if (string.IsNullOrWhiteSpace(clientCode)) { return cacheValue; }

            clientCode = clientCode.Trim().ToUpper();

            // if value exists in cache
            if (this.TryGet(clientCode, out cacheValue))
            {
                return cacheValue;
            }
            else
            {
                // Key not in cache, update cache and try again
                await UpdateCache();

                // if still not there, then return empty string
                return this.TryGet(clientCode, out cacheValue) ? cacheValue : string.Empty;
            }
        }

        private async Task UpdateCache()
        {
            // IEdi835Repository is a scoped service,
            // but this class is a singleton so a scope is needed to use IEdi835Repository
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var edi835Repository = scope.ServiceProvider.GetService<IEdi835Repository>();

                foreach (var config in await edi835Repository.GetEdi835Configurations())
                {
                    // Skip any nulls
                    if (string.IsNullOrWhiteSpace(config.Parser) || string.IsNullOrWhiteSpace(config.ClientCode))
                    {
                        continue;
                    }

                    this.AddOrUpdate(config.ClientCode, config.Parser, (k) => config.Parser);
                }
            }                       
        }
    }
}
