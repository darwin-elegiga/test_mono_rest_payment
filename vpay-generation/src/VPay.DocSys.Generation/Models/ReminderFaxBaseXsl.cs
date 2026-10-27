using System.IO;
using System.Reflection;

namespace VPay.DocSys.Generation.Models
{
    public class ReminderFaxBaseXsl
    {
        public const string ResourceName = "VPay.DocSys.Generation.fo_base_reminder_fax.xsl";

        public Stream GetXslContentStream() => Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
    }
}
