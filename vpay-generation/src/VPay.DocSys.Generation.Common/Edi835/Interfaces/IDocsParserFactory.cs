using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Edi835
{
    public interface IDocsParserFactory
    {
        /// <summary>
        /// Instantiates an object of type <see cref="IEdi835DocsLocator"/> cooresponding to the clientCode using factory pattern
        /// </summary>
        /// <param name="clientCode">The TPA client code. Ex: SWK, HEQTN, RC</param>
        /// <returns>An object of type <see cref="IEdi835DocsLocator"/></returns>
        Task<IEdi835DocsLocator> GetDocsParser(string clientCode);
    }
}
