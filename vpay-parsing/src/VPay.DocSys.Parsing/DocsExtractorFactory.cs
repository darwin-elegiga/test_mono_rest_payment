using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;

namespace VPay.DocSys.Parsing
{
    public class DocsExtractorFactory : IDocsExtractorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Edi835ConfigurationCache _edi835ConfigurationCache;
        public DocsExtractorFactory(
            IServiceProvider serviceProvider,
            Edi835ConfigurationCache cache)
        {
            _serviceProvider = serviceProvider;
            _edi835ConfigurationCache = cache;
        }

        /// <inheritdoc/>
        public async Task<IDocsExtractor> GetDocsExtractor(string clientCode)
        {
            return await _edi835ConfigurationCache.GetParserFromClientCode(clientCode) switch
            {
                Edi835Parser.Swk   =>      ActivatorUtilities.CreateInstance<DocsExtractorSwk>(_serviceProvider),
                Edi835Parser.Heqtn =>      ActivatorUtilities.CreateInstance<DocsExtractorHeqtn>(_serviceProvider),
                Edi835Parser.DirectSpec => ActivatorUtilities.CreateInstance<DocsExtractorDirectSpec>(_serviceProvider),
                Edi835Parser.DualSpec =>   ActivatorUtilities.CreateInstance<DocsExtractorDualSpec>(_serviceProvider),
                Edi835Parser.RedCard =>    null,
                _ => null
            };
        }       
    }   
}
