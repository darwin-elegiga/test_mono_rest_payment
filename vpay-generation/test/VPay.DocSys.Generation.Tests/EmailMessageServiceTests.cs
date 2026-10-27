using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Common.Models;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Tests.TestModels;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class EmailMessageServiceTests
    {
        public class CreateArchiveForXmlString
        {
            private readonly MoqFileSystem _fileSystem = new MoqFileSystem();
            private readonly Mock<IWarrantyPdfCreator> _warrantyPdfCreator = new Mock<IWarrantyPdfCreator>();
            private readonly Mock<IEmailMessageXmlReader> _emailMessageXmlReader = new Mock<IEmailMessageXmlReader>();
            private readonly Mock<IStorageManager> _storageManager = new Mock<IStorageManager>();
            private readonly Mock<IPdfTools> _pdfTools = new Mock<IPdfTools>();

            private readonly DocSysSettings _docSysSettings = new DocSysSettings
            {
                ResultPath = @"C:\test"
            };

            private readonly Mock<ILogger<EmailMessageService>> _logger = new Mock<ILogger<EmailMessageService>>(MockBehavior.Loose);

            private IEmailMessageService SUT => new EmailMessageService(
                _emailMessageXmlReader.Object,
                _warrantyPdfCreator.Object,
                _fileSystem,
                _storageManager.Object,
                _pdfTools.Object,
                _docSysSettings,
                _logger.Object);

            public CreateArchiveForXmlString()
            {
                _emailMessageXmlReader
                    .Setup(_ => _.ReadXml(It.IsAny<string>()))
                    .Returns((string s) => new EmailMessageData(new NotificationRequestXmlDto
                    {
                        Control = new ControlXmlDto
                        {
                            ClientCode = "TEST-TPA",
                            Type = "TEST-TYPE",
                            Application = "TEST-APPLICATION",
                            NotificationId = "789789",
                            BillingEntity = "TEST-BE",
                        },
                        SendTo = new SendToXmlDto
                        {
                            Fax = "1234567890"
                        },
                        Card = new CardXmlDto
                        {
                            LoadTransactionId = "456456",
                        }
                    })
                    {
                        Xml = XDocument.Parse(s)
                    });

                _warrantyPdfCreator
                    .Setup(_ => _.CreatePdfStream(It.IsAny<string>(), It.IsAny<XNode>()))
                    .ReturnsAsync((string s, XNode x) => new MemoryStream(x.ToString().Select(Convert.ToByte).ToArray()));

                _storageManager
                    .Setup(_ => _.CreateArchive(It.IsAny<IArchiveFile>(), It.IsAny<List<IGeneratedFile>>(), It.IsAny<bool>(), null))
                    .ReturnsAsync(-1L);

                _storageManager
                    .Setup(_ => _.GetArchiveForArchiveFileId(It.IsAny<long>(), It.IsAny<bool>()))
                    .ReturnsAsync(() => new FaxArchiveFile("TEST-TPA", 456456, "789789")
                    {
                        ArchiveFileId = -1,
                        FileName = "TEST-FILE",
                        PathName = "TEST-PATH",
                        ArchiveDetails = new List<IArchiveDetail>() { new ArchiveDetail() { FileName = "789789.pdf" } }
                    });
            }

            [Fact]
            public async Task ShouldBeSuccessful()
            {
                var xml = XDocument.Parse("<test></test>");

                var result = await SUT.CreateArchiveForWarrantyXmlString(xml.ToString());

                _emailMessageXmlReader.VerifyAll();
                _warrantyPdfCreator.VerifyAll();
                _pdfTools.VerifyAll();
                _storageManager.VerifyAll();
                _storageManager.Verify(_ => _.CreateArchive(It.IsAny<IArchiveFile>(), It.Is<List<IGeneratedFile>>(x => x.Count == 1), true, null), Times.Once);
                _storageManager.Verify(_ => _.GetArchiveForArchiveFileId(-1L, true), Times.Once);

                Assert.Equal(-1, result.ArchiveFileId);
                Assert.Equal(@"789789.pdf", result.ArchiveDetails.FirstOrDefault().FileName);
                Assert.Equal(@"TEST-PATH", result.PathName);
            }
        }
    }
}
