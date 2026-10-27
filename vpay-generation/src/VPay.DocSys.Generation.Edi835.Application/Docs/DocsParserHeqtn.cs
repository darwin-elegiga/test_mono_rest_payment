using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Generation.Common.Helpers;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835
{
    public class DocsParserHeqtn : IEdi835Creator, IEdi835DocsLocator
    {
        private readonly Edi835ServiceSettings _edi835ServiceSettings;
        private readonly IFileSystem _fileSystem;

        public DocsParserHeqtn( IFileSystem fileSystem, Edi835ServiceSettings edi835ServiceSettings)
        {
            _edi835ServiceSettings = edi835ServiceSettings;
            _fileSystem = fileSystem;
        }
        public Task<DocsFile> LocateContent(DS.Edi835 message, CancellationToken cancellationToken = default)
        {
            // Attempt to find the .docs file created and written out in parsing:

            var fileLocation = _fileSystem.Path.Combine(
                message.GetExpectedDocsFolder(_edi835ServiceSettings.DocsFolder),
                message.GetTempFilename("docs")
                );
            if (!_fileSystem.File.Exists(fileLocation))
            {
                throw new FileNotFoundException("Unable to locate .docs file at expected location.", fileLocation);
            }
            var content = _fileSystem.File.ReadAllText(fileLocation);
            var docsFile = new DocsFile { Content = content, Filename = message.GetFilename("docs") };
            return Task.FromResult(docsFile);
        }



        public async Task<string> CreateContent(Edi835CreationMessage message, CancellationToken cancellationToken = default)
        {
            if (!_fileSystem.File.Exists(message.RawFilePath))
            {
                throw new FileNotFoundException("Unable to find client raw file", message.RawFilePath);
            }
            XElement rawFile = XElement.Load(message.RawFilePath);
            var matchingPayment = rawFile
                .Descendants("PmtRec")
                .Where(_ => message.UserKey.ToUpper().Contains(_.Element("PaymentID").Value.ToUpper()))
                .FirstOrDefault();
            if (matchingPayment == null)
            {
                throw new Exception("Payment not found");
            }
            var content = await GenerateContentFromXml(matchingPayment);
            return content;
        }

        private Task<string> GenerateContentFromXml(XElement payment)
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
            var content = string.Join("|", fields) + Environment.NewLine;
            return Task.FromResult(content);
        }


    }
}
