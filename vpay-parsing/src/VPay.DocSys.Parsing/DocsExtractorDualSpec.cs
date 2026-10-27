using VPay.DocSys.Parsing.Common;

namespace VPay.DocSys.Parsing
{
    public class DocsExtractorDualSpec : DocsExtractorDirectSpec
    {
        public DocsExtractorDualSpec(Edi835ServiceSettings edi835ServiceSettings, ITarExtractor tarExtractor)
            : base(edi835ServiceSettings, tarExtractor)
        {
        }
    }
}
