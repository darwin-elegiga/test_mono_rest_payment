using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface ISubFileParserService
    {
        IDictionary<string, SubstituteDataInfo> ParseFile(string filePath);
    }
}
