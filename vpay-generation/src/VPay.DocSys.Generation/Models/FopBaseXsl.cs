using System.IO;
using System.Reflection;

namespace VPay.DocSys.Generation.Models
{
    public class FopBaseXsl
    {
        public const string ResourceName = "VPay.DocSys.Generation.fo_base_transform.xsl";

        public Stream GetXslContentStream() => Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
    }
}
