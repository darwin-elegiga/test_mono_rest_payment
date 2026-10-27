using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    public interface IEobXmlParser
    {
        Task<IDictionary<string, dynamic>> EobToDictionaryAsync(Stream stream);
    }
}
