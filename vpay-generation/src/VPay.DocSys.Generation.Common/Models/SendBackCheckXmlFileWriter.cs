using System;
using System.IO.Abstractions;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Class to convert the <see cref="CheckInfo"/> object into Custom Send Back xml for creating checks
    /// </summary>
    public interface ISendBackCheckXmlFileWriter
    {
        /// <summary>
        /// Creates the xml file needed for generating custom send back check documents
        /// </summary>
        /// <param name="directory">directory to create file</param>
        /// <param name="checkInfo"><see cref="CheckInfo"/> to convert into xml</param>
        /// <returns>file name with path</returns>
        string CreateXmlFile(string directory, CheckInfo checkInfo);
    }

    public class SendBackCheckXmlFileWriter : ISendBackCheckXmlFileWriter
    {
        private static XmlSerializer CheckFileSerializer => new XmlSerializer(typeof(CheckFile));

        private static XmlWriterSettings XmlWriterSettings => new XmlWriterSettings
        {
            Encoding = Encoding.UTF8
        };

        private readonly IFileSystem _fileSystem;
        private readonly ILogger<SendBackCheckXmlFileWriter> _logger;

        public SendBackCheckXmlFileWriter(
            IFileSystem fileSystem,
            ILogger<SendBackCheckXmlFileWriter> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;

            ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ThrowIf.IsNull(logger, nameof(logger));
        }

        public string CreateXmlFile(string directory, CheckInfo checkInfo)
        {
            ThrowIf.IsNullOrEmpty(directory, nameof(directory));
            ThrowIf.IsNull(checkInfo, nameof(checkInfo));

            var filePath = _fileSystem.Path.Combine(directory, $"{checkInfo.SETransactionId}.xml");

            using (var xw = XmlWriter.Create(filePath, XmlWriterSettings))
            {
                CheckFileSerializer.Serialize(xw, GetCheckFile(checkInfo));
            }

            _logger.LogInformation($"Created {filePath} for Custom Send Back check documents");
            return filePath;
        }

        /// <summary>
        /// Create <see cref="CheckFile"/> using <see cref="CheckInfo"/>
        /// </summary>
        /// <param name="checkInfo">input data</param>
        /// <returns>minimum data filled to create valid xml for <code>/home/bogner/checkprinting_rlab/check_maker/__main__.py</code></returns>
        private static CheckFile GetCheckFile(CheckInfo checkInfo)
        {
            return new CheckFile
            {
                checkrun = new rootCheckrun
                {
                    header = new rootCheckrunHeader
                    {
                        bank = new rootCheckrunHeaderBank
                        {
                            account = new rootCheckrunHeaderBankAccount
                            {
                                accountnumber = checkInfo.BankAccountNumber,
                                routingnumber = checkInfo.BankRoutingNumber
                            },
                            address = checkInfo.BankAddress,
                            fractionalroute = checkInfo.BankFractionalRoute,
                            name = checkInfo.BankName,
                        },
                        source = new rootCheckrunHeaderSource
                        {
                            address = checkInfo.SourceAddress,
                            city = checkInfo.SourceCity,
                            name = checkInfo.SourceName,
                            state = checkInfo.SourceState,
                            zipcode = checkInfo.SourceZipcode,
                        },
                    },
                    be = checkInfo.BE,
                    tpa = checkInfo.TPA,
                    checks = new[]
                    {
                        new rootCheckrunCheck
                        {
                            accountchecknumber = checkInfo.CheckNumber,
                            amount = checkInfo.CheckAmount,
                            checkdate = checkInfo.CheckDate,
                            checknumber = checkInfo.CheckNumber,
                            docguid = checkInfo.DocGuid,
                            seixid = checkInfo.SETransactionId,
                            payto = checkInfo.Payto,
                            sendtype = checkInfo.SendType,
                            voidAfter = checkInfo.VoidAfter.ToString(),
                            mailto = new rootCheckrunCheckMailto
                            {
                                addressLine1 = checkInfo.MailtoAddressline1,
                                addressLine2 = checkInfo.MailtoAddressline2,
                                addressLine3 = checkInfo.MailtoAddressline3,
                                addressLine4 = checkInfo.MailtoAddressline4,
                            },
                            eob = new rootCheckrunCheckEob
                            {
                                notificationRequest = new rootCheckrunCheckEobNotificationRequest
                                {
                                    coveredItem = new rootCheckrunCheckEobNotificationRequestCoveredItem
                                    {
                                        userDisplay1 = checkInfo.ClientPaymentNumber,
                                    },
                                    checkinfo = new rootCheckrunCheckEobNotificationRequestCheckinfo
                                    {
                                        checkstatus = checkInfo.CheckStatus,
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }
    }
}
