namespace VPay.DocSys.Generation.Common
{
    public interface IFaxMessageXmlReader
    {
        /// <summary>
        /// Reads and validates the warranty xml string
        /// </summary>
        /// <param name="xmlString">warranty fax xml string</param>
        /// <returns>warranty fax data object</returns>
        FaxMessageData ReadXml(string xmlString);
    }
}
