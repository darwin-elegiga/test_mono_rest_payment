using System.Threading.Tasks;

namespace VPay.DocSys.Parsing.Common
{
    public interface IDocsExtractorFactory
    {
        /// <summary>
        /// Instantiates an object of type <see cref="IDocsExtractor"/> cooresponding to the clientCode using factory pattern
        /// </summary>
        /// <param name="clientCode">The TPA client code. Ex: SWK, HEQTN, RC</param>
        /// <returns>An object of type <see cref="IDocsExtractor"/></returns>
        Task<IDocsExtractor> GetDocsExtractor(string clientCode);
    }
}
