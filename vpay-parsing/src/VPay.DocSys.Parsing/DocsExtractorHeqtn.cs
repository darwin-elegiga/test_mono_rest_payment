using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;

namespace VPay.DocSys.Parsing
{
    public class DocsExtractorHeqtn : IDocsExtractor
    {
        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly ITarExtractor _tarExtractor;
        private readonly string _lineEnding;
        public DocsExtractorHeqtn(Edi835ServiceSettings edi835ServiceSettings, ITarExtractor tarExtractor)
        {
            _lineEnding = edi835ServiceSettings.LineEnding;
            _tarExtractor = tarExtractor;
        }

        /// <inheritdoc/>
        public IEnumerable<Edi835ParsedDocsContent> ExtractDocs(string tarDataFilename, string inputFilename, List<DocumentTransactionMessage> messages)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.Async = false; // Set to true once we've got it working as expected.

            using (var tarInputStream = _tarExtractor.StreamTarEntry(tarDataFilename, inputFilename, _encoding))
            using (var reader = XmlReader.Create(tarInputStream, settings))
            {

                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element) { continue; }
                    if (reader.Name == "PmtRec")
                    {
                        var pmtRec = XElement.Load(reader.ReadSubtree());
                        var userKey = (pmtRec.XPathSelectElement("//PaymentID")?.Value.Trim().ToUpper() ?? "");
                        var currentMessage = messages.FirstOrDefault(_ => _.StatusRecord.USERKEY.Trim().ToUpper().Equals(userKey));
                        if (currentMessage == null) { continue; } // Not found in messages, move to next one.
                        yield return new Edi835ParsedDocsContent(currentMessage, GenerateContentFromXml(pmtRec));
                    }

                }
                tarInputStream.Close();
            }
        }

        private string GenerateContentFromXml(XElement payment)
        {
            List<string> fields = new List<string>(45);
            fields.Add(payment.Element("PmtMethod").Value);
            fields.Add(payment.Element("PaymentID").Value);
            fields.Add(payment.Element("Amount").Value);
            fields.Add(payment.Element("AccountType").Value);
            fields.Add(payment.Element("DateOfService").Value);
            fields.Add(payment.Element("MemberId").Value);
            fields.Add(payment.Element("MemberName").Value);
            fields.Add(payment.Element("PatientId").Value);
            fields.Add(payment.Element("PatientName").Value);
            fields.Add(payment.Element("PatientBirthDate").Value);
            fields.Add(payment.Element("ClaimNumber").Value);
            fields.Add(payment.Element("GroupName").Value);
            fields.Add(payment.Element("ReferenceId").Value);
            fields.Add(payment.Element("TypeOfService").Value);
            fields.Add(payment.Element("Provider").Value);
            fields.Add(payment.Element("AltClaimNumber").Value);

            fields.Add(payment.Element("Administrator").Element("Name").Element("Name1").Value);
            fields.Add(payment.Element("Administrator").Element("PostAddr").Element("Addr1").Value);
            fields.Add(payment.Element("Administrator").Element("PostAddr").Element("Addr2").Value);
            fields.Add(payment.Element("Administrator").Element("PostAddr").Element("City").Value);
            fields.Add(payment.Element("Administrator").Element("PostAddr").Element("StateProv").Value);
            fields.Add(payment.Element("Administrator").Element("PostAddr").Element("PostalCode").Value);

            fields.Add(payment.Element("ClaimPayee").Element("Name").Element("Name1").Value);
            fields.Add(payment.Element("ClaimPayee").Element("TIN").Value);
            fields.Add(payment.Element("ClaimPayee").Element("MasterPayeeId").Value);

            fields.Add(payment.Element("ClaimPayee").Element("PostAddr").Element("Addr1").Value);
            fields.Add(payment.Element("ClaimPayee").Element("PostAddr").Element("Addr2").Value);
            fields.Add(payment.Element("ClaimPayee").Element("PostAddr").Element("City").Value);
            fields.Add(payment.Element("ClaimPayee").Element("PostAddr").Element("StateProv").Value);
            fields.Add(payment.Element("ClaimPayee").Element("PostAddr").Element("PostalCode").Value);

            fields.Add(payment.Element("PatientAddress").Element("Addr1").Value);
            fields.Add(payment.Element("PatientAddress").Element("Addr2").Value);
            fields.Add(payment.Element("PatientAddress").Element("City").Value);
            fields.Add(payment.Element("PatientAddress").Element("StateProv").Value);
            fields.Add(payment.Element("PatientAddress").Element("PostalCode").Value);

            fields.Add(payment.Element("PatientAccountNumber").Value);
            fields.Add(payment.Element("PlanYear").Value);
            fields.Add(payment.Element("MemberResponsibility").Value);
            fields.Add(payment.Element("DeniedAmount").Value);
            fields.Add(payment.Element("DeniedReason").Value);
            fields.Add(payment.Element("Deductible").Value);
            fields.Add(payment.Element("ProcessedDate").Value);

            var procCodes = string.Join(",", payment
                .Element("Procedures")
                .Descendants("Procedure")
                .Select(c => c.Element("Code").Value)
                .ToArray());
            var procAmounts = string.Join(",", payment
                 .Element("Procedures")
                 .Descendants("Procedure")
                 .Select(c => c.Element("Amount").Value)
                 .ToArray());

            fields.Add(procCodes);
            fields.Add(payment.Element("OutOfNetwork").Value);
            fields.Add(procAmounts);
            var content = string.Join("|", fields) + _lineEnding;
            return content;
        }
    }
}
