using System.IO;
using System.Reflection;

namespace VPay.DocSys.Generation.Models
{
    public class EftFaxFopXsl
    {
        public const string ResourceName = "VPay.DocSys.Generation.fo_base_eft_fax.xsl";

        public Stream GetXslContentStream() => Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
    }
}
