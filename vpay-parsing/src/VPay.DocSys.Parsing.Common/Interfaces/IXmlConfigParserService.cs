namespace VPay.DocSys.Parsing.Common.Interfaces
{
    /// <summary>
    /// XML Configuration File Parser Server
    /// </summary>
    public interface IXmlConfigParserService
    {
        /// <summary>
        /// Reads the XML configuration files
        /// </summary>
        /// <param name="tpaCode">tpa to use for initial lookup</param>
        /// <param name="beCode">be to use for secondary configuration values, may not have any</param>
        ConfigurationXmlDto ReadXmlConfiguration(string tpaCode, string beCode);
    }
}
