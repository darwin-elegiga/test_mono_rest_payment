using System.Xml.Serialization;

namespace VPay.DocSys.Parsing.Common
{
    [XmlRoot(ElementName = "field")]
    public class FieldConfigurationXmlDto
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "type")]
        public string Type { get; set; }

        [XmlElement(ElementName = "source")]
        public string Source { get; set; }

        [XmlElement(ElementName = "value")]
        public string Value { get; set; }

        [XmlElement(ElementName = "unit")]
        public string Unit { get; set; }

        [XmlElement(ElementName = "delimiter")]
        public string Delimiter { get; set; }

        [XmlElement(ElementName = "basetype")]
        public string BaseType { get; set; }

        [XmlElement(ElementName = "dependencies")]
        public string Dependencies { get; set; }

        [XmlElement(ElementName = "position")]
        public string Position { get; set; }

        [XmlElement(ElementName = "style")]
        public string Style { get; set; }

        [XmlElement(ElementName = "expression")]
        public string Expression { get; set; }
    }
}
