using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Edi835
{
    public class DocsParserFactory : IDocsParserFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Edi835ConfigurationCache _edi835ConfigurationCache;
        public DocsParserFactory(
            IServiceProvider serviceProvider,
            Edi835ConfigurationCache cache)
        {
            _serviceProvider = serviceProvider;
            _edi835ConfigurationCache = cache;
        }

        /// <inheritdoc/>
        public async Task<IEdi835DocsLocator> GetDocsParser(string clientCode)
        {
            return await _edi835ConfigurationCache.GetParserFromClientCode(clientCode) switch
            {
                Edi835Parser.Swk =>        ActivatorUtilities.CreateInstance<DocsParserSwk>(_serviceProvider),
                Edi835Parser.Heqtn =>      ActivatorUtilities.CreateInstance<DocsParserHeqtn>(_serviceProvider),
                Edi835Parser.RedCard =>    ActivatorUtilities.CreateInstance<DocsParserRedCard>(_serviceProvider),
                Edi835Parser.DirectSpec => ActivatorUtilities.CreateInstance<DocsParserDirectSpec>(_serviceProvider),
                Edi835Parser.DualSpec =>   ActivatorUtilities.CreateInstance<DocsParserDualSpec>(_serviceProvider),
                _ =>                       ActivatorUtilities.CreateInstance<DocsParserNoOp>(_serviceProvider),
            };
        }
    }        
}
