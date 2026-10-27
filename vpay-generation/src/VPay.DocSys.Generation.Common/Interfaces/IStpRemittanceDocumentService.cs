using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.XPath;
using VPay.DocSys.Generation.Common.Models;

namespace VPay.DocSys.Generation.Common.Interfaces;

public interface IStpRemittanceDocumentService
{
    Task<(IEnumerable<StpRemittanceGeneratedFile> files, bool remittanceSent)> GenerateAndSendRemittanceDocument(DocGenParamInternal param, long transactionId, XPathDocument eobXml);
}
