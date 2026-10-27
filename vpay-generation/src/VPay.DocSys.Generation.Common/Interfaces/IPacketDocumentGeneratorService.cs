using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public interface IPacketDocumentGeneratorService
    {
        /// <summary>
        /// Generate Packet Documents
        /// </summary>
        /// <param name="packetDocs"><see cref="IEnumerable{PacketDocument}"/> to generate</param>
        /// <param name="param">parameters used in document generation</param>
        /// <returns><see cref="IEnumerable{EobGeneratedFile}"/></returns>
        Task<IEnumerable<PacketGeneratedFile>> GeneratePacketDocuments(IEnumerable<PacketDocument> packetDocs, DocGenParamInternal param);
    }
}
