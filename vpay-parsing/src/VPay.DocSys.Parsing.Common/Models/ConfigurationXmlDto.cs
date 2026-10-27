using System.Collections.Generic;
using System.Xml.Serialization;

namespace VPay.DocSys.Parsing.Common
{
    [XmlRoot(ElementName = "Configuration")]
    public class ConfigurationXmlDto
    {
        [XmlArray(ElementName = "common")]
        [XmlArrayItem(ElementName = "field")]
        public List<FieldConfigurationXmlDto> Common { get; set; }

        [XmlArray(ElementName = "vcard")]
        [XmlArrayItem(ElementName = "field")]
        public List<FieldConfigurationXmlDto> VCard { get; set; }

        [XmlArray(ElementName = "check")]
        [XmlArrayItem(ElementName = "field")]
        public List<FieldConfigurationXmlDto> Check { get; set; }

        [XmlArray(ElementName = "eft")]
        [XmlArrayItem(ElementName = "field")]
        public List<FieldConfigurationXmlDto> EFT { get; set; }

        [XmlArray(ElementName = "npf")]
        [XmlArrayItem(ElementName = "field")]
        public List<FieldConfigurationXmlDto> NPF { get; set; }

        [XmlArray(ElementName = "clccp")]
        [XmlArrayItem(ElementName = "field")]
        public List<FieldConfigurationXmlDto> CLCCP { get; set; }

        [XmlArray(ElementName = "clcck")]
        [XmlArrayItem(ElementName = "field")]
        public List<FieldConfigurationXmlDto> CLCCK { get; set; }

        [XmlArray(ElementName = "clcft")]
        [XmlArrayItem(ElementName = "field")]
        public List<FieldConfigurationXmlDto> CLCFT { get; set; }

        [XmlArray(ElementName = "clvch")]
        [XmlArrayItem(ElementName = "field")]
        public List<FieldConfigurationXmlDto> CLVCH { get; set; }
    }
}
