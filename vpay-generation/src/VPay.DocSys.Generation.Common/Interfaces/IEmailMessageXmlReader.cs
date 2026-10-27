using VPay.DocSys.Generation.Common.Models;

namespace VPay.DocSys.Generation.Common.Interfaces;

public interface IEmailMessageXmlReader
{
    /// <summary>
    /// Reads and validates the warranty xml string
    /// </summary>
    /// <param name="xmlString">warranty email xml string</param>
    /// <returns>warranty fax data object</returns>
    EmailMessageData ReadXml(string xmlString);
}
