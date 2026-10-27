using System.Collections.Generic;
using System.IO;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Common
{
    public class DocGenParamInternal
    {
        public DocGenParam DocGenParam { get; set; }

        public DocSysConfig Config { get; set; }

        public string SavePath { get; set; }

        public string EobXmlFilePath { get; set; }

        public string EobPdfFilePath { get; set; }

        public IDictionary<string, dynamic> EobAsDictionary { get; set; }

        public ReissueEOBData ReIssueData { get; set; }

        public bool IsReIssue()
        {
            return string.Equals(DocGenParam?.StatusRecord?.CHKSTS, "RI", System.StringComparison.OrdinalIgnoreCase);
        }

        public bool IsStraightThruProcessing()
        {
            return string.Equals(DocGenParam?.StatusRecord.NETWORKTYPE, "STP", System.StringComparison.OrdinalIgnoreCase);
        }

        public string GetSaveFileName(string fileExtension)
        {
            return EobXmlFilePath.Replace(".eob.xml", fileExtension);
        }

        public string GetVPaygeFileName(VPaygeDocumentType document, bool isRedCard)
        {
            if (isRedCard)
            {
                return this.GetRedCardVPaygeFileName(document);
            }

            var fileExtension = document.GetAttribute<DocFileExtensionAttribute>();
            if (fileExtension == null)
            {
                return GetSaveFileName(Constants.FileExtensions.VPaygeDefaultPdf);
            }
            return GetSaveFileName(fileExtension.FileExtension);
        }

        private string GetRedCardVPaygeFileName(VPaygeDocumentType document)
        {
            if (this.IsReIssue())
            {
                return Path.Combine(Path.GetDirectoryName(EobXmlFilePath), $"{DocGenParam.InputFile}.{DocGenParam.StatusRecord.GUID}{document.ToRedCardFileExtension(true)}");
            }
            else
            {
                return Path.Combine(Path.GetDirectoryName(EobXmlFilePath), $"{DocGenParam.CDate}.{DocGenParam.InputFile}.{DocGenParam.StatusRecord.GUID}{document.ToRedCardFileExtension(false)}");
            }
        }
    }
}
