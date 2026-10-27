using System;
using System.Linq;
using System.Xml.Serialization;

namespace VPay.DocSys.Generation.Models
{
    /// <summary>
    /// This is used to get the custom_pdf xml to a class structure. XmlSerializer requires the class to be public.
    /// </summary>
    [XmlRoot("custom_pdfs")]
    public class EobXmlCustomPdfs
    {
        private string _method;
        private string _duplexPrint;
        private string _order = "";
        private CustomPdfFile[] _files = null;

        [XmlElement("method")]
        public string Method
        {
            get => _method == null ? null : _method.ToUpper();
            set => _method = value;
        }

        [XmlElement("duplex_print")]
        public string DuplexPrint
        {
            get => _duplexPrint == null ? null : _duplexPrint.ToUpper();
            set => _duplexPrint = value;
        }

        [XmlIgnore]
        public int? Order {
            get { return !string.IsNullOrWhiteSpace(_order) ? int.Parse(_order) : default(int?); }
            set
            {
                if (value == null)
                {
                    _order = "";
                } else
                {
                    _order = value.Value.ToString();
                }
            }
        }

        [XmlElement("order", IsNullable = true)]
        public string OrderAsString
        {
            get => _order;
            set => _order = value;
        }

        [XmlElement("files", IsNullable = true)]
        public CustomPdfFile[] Files
        {
            get => _files;
            set => _files = (value == null) ? value : value.Where(x => !String.IsNullOrWhiteSpace(x.File)).ToArray();
        }

    }


    [XmlType(AnonymousType = true)]
    public class CustomPdfFile
    {

        [XmlElement("file")]
        public string File { get; set; }
    }
}
