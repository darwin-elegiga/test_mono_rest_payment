using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Data.Db2.Abstractions.Trn835Data;
using VPay.Data.Db2.Abstractions.Trn835Data.Models;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Enums;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Common.Models.Sql;
using VPay.DocSys.Generation.Data;
using VPay.DocSys.Generation.Tests.TestModels;
using Xunit;

namespace VPay.DocSys.Generation.Tests;

public class StpRemittanceDocumentServiceTests
{
    private readonly Mock<IStpRemittanceRepository> _stpRemittanceRepository = new Mock<IStpRemittanceRepository>();
    private readonly Mock<ITrn835DataRepository> _trn835DataRepository = new Mock<ITrn835DataRepository>();
    private readonly MoqFileSystem _fileSystem = new MoqFileSystem();
    private readonly Mock<ILogger<StpRemittanceDocumentService>> _logger = new Mock<ILogger<StpRemittanceDocumentService>>(MockBehavior.Loose);
    private readonly Mock<IRemittancePartnerNetworkApiClient> _remittancePartnerNetworkApiClient = new Mock<IRemittancePartnerNetworkApiClient>();

    private IStpRemittanceDocumentService _service;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async ValueTask GenerateAndSendRemittanceDocument_ShouldReturnRemittanceSentResult(bool documentSent)
    {
        var tpaAddress = new TpaAddress()
        {
            Address = "123",
            City = "City",
            State = "ST",
            Zip = "9999",
            Tpa = "ABC"
        };

        _stpRemittanceRepository
            .Setup(x => x.GetTpaAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(tpaAddress))
            .Verifiable();

        var trnData = new Trn835Data()
        {
            Segment = "XXXXXXX"
        };

        _trn835DataRepository
            .Setup(x => x.GetTrn835DataByNonFundedPaymentDetailId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(trnData))
            .Verifiable();

        _remittancePartnerNetworkApiClient
            .Setup(x => x.PostRemittanceDocument(It.IsAny<RemittanceNetworkEnrollmentTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((documentAccepted: documentSent, responseBody: "")))
            .Verifiable();

        var docParam = new DocGenParamInternal()
        {
            SavePath = "/tmp/Result/",
            DocGenParam = new DocGenParam()
            {
                StatusRecord = new DocSys.Common.StatusRecord()
                {
                    NFPYDT_ID = "0",
                    TPA = "ABC",
                    NETWORKNAME = "TEST",
                    NETWORKENROLLMENTTYPE = "C2B",
                    TIN = "9999999"
                }
            }
        };

        _fileSystem.AddDirectory(docParam.SavePath);

        var transactionId = 1;
        var eobXml = new XPathDocument(new StringReader("<eob></eob>"));

        _service = new StpRemittanceDocumentService(_stpRemittanceRepository.Object, _trn835DataRepository.Object,
            _fileSystem, _logger.Object, _remittancePartnerNetworkApiClient.Object);

        var result = await _service.GenerateAndSendRemittanceDocument(docParam, transactionId, eobXml);

        var remittanceSent = result.remittanceSent;
        var files = result.files;

        remittanceSent.Should().Be(documentSent);
        files.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("ABCD-12345678")]
    [InlineData("ABCD 12345678")]
    public async ValueTask GenerateAndSendRemittanceDocument_ShouldReturnRemittanceWithNetworkUuid(string networkUuid)
    {
        bool documentSent = true; // Simulate document sent successfully

        var tpaAddress = new TpaAddress()
        {
            Address = "123",
            City = "City",
            State = "ST",
            Zip = "9999",
            Tpa = "ABC"
        };

        _stpRemittanceRepository
            .Setup(x => x.GetTpaAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(tpaAddress))
            .Verifiable();

        var trnData = new Trn835Data()
        {
            Segment = "XXXXXXX"
        };

        _trn835DataRepository
            .Setup(x => x.GetTrn835DataByNonFundedPaymentDetailId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(trnData))
            .Verifiable();

        _remittancePartnerNetworkApiClient
            .Setup(x => x.PostRemittanceDocument(It.IsAny<RemittanceNetworkEnrollmentTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((documentAccepted: documentSent, responseBody: "")))
            .Verifiable();

        var docParam = new DocGenParamInternal()
        {
            SavePath = "/tmp/Result/",
            DocGenParam = new DocGenParam()
            {
                StatusRecord = new DocSys.Common.StatusRecord()
                {
                    NFPYDT_ID = "0",
                    TPA = "ABC",
                    NETWORKNAME = "TEST",
                    NETWORKENROLLMENTTYPE = "C2B",
                    TIN = "9999999",
                    SS_SID = networkUuid // Use the provided network UUID
                }
            }
        };

        _fileSystem.AddDirectory(docParam.SavePath);

        var transactionId = 1;
        var eobXml = new XPathDocument(new StringReader("<eob></eob>"));

        _service = new StpRemittanceDocumentService(_stpRemittanceRepository.Object, _trn835DataRepository.Object,
            _fileSystem, _logger.Object, _remittancePartnerNetworkApiClient.Object);

        var result = await _service.GenerateAndSendRemittanceDocument(docParam, transactionId, eobXml);

        var remittanceSent = result.remittanceSent;
        var files = result.files;

        var fileName = $"{transactionId}.stp.c2b.remittance.json";
        var content = _fileSystem.File.ReadAllText(Path.Combine(docParam.SavePath, fileName));

        remittanceSent.Should().Be(documentSent);
        files.Should().HaveCount(2);

        content.Should().Contain($"\"uuid\": \"{networkUuid}\"");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async ValueTask GenerateAndSendRemittanceDocument_ShouldReturnRemittanceWithNullNetworkUuid(string networkUuid)
    {
        bool documentSent = true; // Simulate document sent successfully

        var tpaAddress = new TpaAddress()
        {
            Address = "123",
            City = "City",
            State = "ST",
            Zip = "9999",
            Tpa = "ABC"
        };

        _stpRemittanceRepository
            .Setup(x => x.GetTpaAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(tpaAddress))
            .Verifiable();

        var trnData = new Trn835Data()
        {
            Segment = "XXXXXXX"
        };

        _trn835DataRepository
            .Setup(x => x.GetTrn835DataByNonFundedPaymentDetailId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(trnData))
            .Verifiable();

        _remittancePartnerNetworkApiClient
            .Setup(x => x.PostRemittanceDocument(It.IsAny<RemittanceNetworkEnrollmentTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((documentAccepted: documentSent, responseBody: "")))
            .Verifiable();

        var docParam = new DocGenParamInternal()
        {
            SavePath = "/tmp/Result/",
            DocGenParam = new DocGenParam()
            {
                StatusRecord = new DocSys.Common.StatusRecord()
                {
                    NFPYDT_ID = "0",
                    TPA = "ABC",
                    NETWORKNAME = "TEST",
                    NETWORKENROLLMENTTYPE = "C2B",
                    TIN = "9999999",
                    SS_SID = networkUuid // Use the provided network UUID
                }
            }
        };

        _fileSystem.AddDirectory(docParam.SavePath);

        var transactionId = 1;
        var eobXml = new XPathDocument(new StringReader("<eob></eob>"));

        _service = new StpRemittanceDocumentService(_stpRemittanceRepository.Object, _trn835DataRepository.Object,
            _fileSystem, _logger.Object, _remittancePartnerNetworkApiClient.Object);

        var result = await _service.GenerateAndSendRemittanceDocument(docParam, transactionId, eobXml);

        var remittanceSent = result.remittanceSent;
        var files = result.files;

        var fileName = $"{transactionId}.stp.c2b.remittance.json";
        var content = _fileSystem.File.ReadAllText(Path.Combine(docParam.SavePath, fileName));

        remittanceSent.Should().Be(documentSent);
        files.Should().HaveCount(2);

        content.Should().NotContain($"\"uuid\"");
    }

    [Fact]
    public async ValueTask GenerateAndSendRemittanceDocument_ShouldReturnFalseWhenThereIsAnException()
    {
        var tpaAddress = new TpaAddress()
        {
            Address = "123",
            City = "City",
            State = "ST",
            Zip = "9999",
            Tpa = "ABC"
        };

        _stpRemittanceRepository
            .Setup(x => x.GetTpaAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(tpaAddress))
            .Verifiable();

        var trnData = new Trn835Data()
        {
            Segment = "XXXXXXX"
        };

        _trn835DataRepository
            .Setup(x => x.GetTrn835DataByNonFundedPaymentDetailId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Throws<Exception>()
            .Verifiable();

        _remittancePartnerNetworkApiClient
            .Setup(x => x.PostRemittanceDocument(It.IsAny<RemittanceNetworkEnrollmentTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((documentAccepted: true, responseBody: "")))
            .Verifiable();

        var docParam = new DocGenParamInternal()
        {
            SavePath = "/tmp/Result/",
            DocGenParam = new DocGenParam()
            {
                StatusRecord = new DocSys.Common.StatusRecord()
                {
                    NFPYDT_ID = "0",
                    TPA = "ABC",
                    NETWORKNAME = "TEST",
                    NETWORKENROLLMENTTYPE = "C2B"
                }
            }
        };

        _fileSystem.AddDirectory(docParam.SavePath);

        var transactionId = 1;
        var eobXml = new XPathDocument(new StringReader("<eob></eob>"));

        _service = new StpRemittanceDocumentService(_stpRemittanceRepository.Object, _trn835DataRepository.Object,
            _fileSystem, _logger.Object, _remittancePartnerNetworkApiClient.Object);

        var result = await _service.GenerateAndSendRemittanceDocument(docParam, transactionId, eobXml);

        var remittanceSent = result.remittanceSent;
        var files = result.files;

        remittanceSent.Should().Be(false);
        files.Should().HaveCount(1);
    }

    [Fact]
    public async ValueTask GenerateAndSendRemittanceDocument_ShouldReturnFalseWhenNetworkEnrollmentTypeIsInvalid()
    {
        var tpaAddress = new TpaAddress()
        {
            Address = "123",
            City = "City",
            State = "ST",
            Zip = "9999",
            Tpa = "ABC"
        };

        _stpRemittanceRepository
            .Setup(x => x.GetTpaAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(tpaAddress))
            .Verifiable();

        var trnData = new Trn835Data()
        {
            Segment = "XXXXXXX"
        };

        _trn835DataRepository
            .Setup(x => x.GetTrn835DataByNonFundedPaymentDetailId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Throws<Exception>()
            .Verifiable();

        _remittancePartnerNetworkApiClient
            .Setup(x => x.PostRemittanceDocument(It.IsAny<RemittanceNetworkEnrollmentTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((documentAccepted: true, responseBody: "")))
            .Verifiable();

        var docParam = new DocGenParamInternal()
        {
            SavePath = "/tmp/Result/",
            DocGenParam = new DocGenParam()
            {
                StatusRecord = new DocSys.Common.StatusRecord()
                {
                    NFPYDT_ID = "0",
                    TPA = "ABC",
                    NETWORKNAME = "TEST",
                    NETWORKENROLLMENTTYPE = "XXX"
                }
            }
        };

        _fileSystem.AddDirectory(docParam.SavePath);

        var transactionId = 1;
        var eobXml = new XPathDocument(new StringReader("<eob></eob>"));

        _service = new StpRemittanceDocumentService(_stpRemittanceRepository.Object, _trn835DataRepository.Object,
            _fileSystem, _logger.Object, _remittancePartnerNetworkApiClient.Object);

        var result = await _service.GenerateAndSendRemittanceDocument(docParam, transactionId, eobXml);

        var remittanceSent = result.remittanceSent;
        var files = result.files;

        remittanceSent.Should().Be(false);
        files.Should().HaveCount(1);
    }

    [Fact]
    public async ValueTask GenerateAndSendRemittanceDocumentForC2B_ShouldPostRemittanceDocument()
    {
        var tpaAddress = new TpaAddress()
        {
            Address = "123",
            City = "City",
            State = "ST",
            Zip = "9999",
            Tpa = "ABC"
        };

        _stpRemittanceRepository
            .Setup(x => x.GetTpaAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(tpaAddress))
            .Verifiable();

        var trnData = new Trn835Data()
        {
            Segment = "XXXXXXX"
        };

        _trn835DataRepository
            .Setup(x => x.GetTrn835DataByNonFundedPaymentDetailId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(trnData))
            .Verifiable();

        _remittancePartnerNetworkApiClient
            .Setup(x => x.PostRemittanceDocument(It.IsAny<RemittanceNetworkEnrollmentTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((documentAccepted: true, responseBody: "")))
            .Verifiable();

        DocGenParamInternal docParam = CreateGenParamInternal("C2B");

        _fileSystem.AddDirectory(docParam.SavePath);

        var transactionId = 1;
        var eobXml = new XPathDocument(new StringReader("<eob></eob>"));

        _service = new StpRemittanceDocumentService(_stpRemittanceRepository.Object, _trn835DataRepository.Object,
            _fileSystem, _logger.Object, _remittancePartnerNetworkApiClient.Object);

        var result = await _service.GenerateAndSendRemittanceDocument(docParam, transactionId, eobXml);

        var remittanceSent = result.remittanceSent;
        var files = result.files;

        _remittancePartnerNetworkApiClient.VerifyAll();
        _remittancePartnerNetworkApiClient.Verify(x => x.PostRemittanceDocument(It.IsAny<RemittanceNetworkEnrollmentTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _remittancePartnerNetworkApiClient.Verify(x => x.ValidateRemittanceDocument(It.IsAny<RemittanceNetworkEnrollmentTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async ValueTask GenerateAndSendRemittanceDocumentForB2B_ShouldValidateRemittanceDocument()
    {
        var tpaAddress = new TpaAddress()
        {
            Address = "123",
            City = "City",
            State = "ST",
            Zip = "9999",
            Tpa = "ABC"
        };

        _stpRemittanceRepository
            .Setup(x => x.GetTpaAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(tpaAddress))
            .Verifiable();

        var trnData = new Trn835Data()
        {
            Segment = "XXXXXXX"
        };

        _trn835DataRepository
            .Setup(x => x.GetTrn835DataByNonFundedPaymentDetailId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(trnData))
            .Verifiable();

        _remittancePartnerNetworkApiClient
            .Setup(x => x.ValidateRemittanceDocument(It.IsAny<RemittanceNetworkEnrollmentTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((documentAccepted: true, responseBody: "")))
            .Verifiable();

        DocGenParamInternal docParam = CreateGenParamInternal("B2B");

        _fileSystem.AddDirectory(docParam.SavePath);

        var transactionId = 1;
        var eobXml = new XPathDocument(new StringReader("<eob></eob>"));

        _service = new StpRemittanceDocumentService(_stpRemittanceRepository.Object, _trn835DataRepository.Object,
            _fileSystem, _logger.Object, _remittancePartnerNetworkApiClient.Object);

        var result = await _service.GenerateAndSendRemittanceDocument(docParam, transactionId, eobXml);

        var remittanceSent = result.remittanceSent;
        var files = result.files;

        _remittancePartnerNetworkApiClient.VerifyAll();
        _remittancePartnerNetworkApiClient.Verify(x => x.PostRemittanceDocument(It.IsAny<RemittanceNetworkEnrollmentTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _remittancePartnerNetworkApiClient.Verify(x => x.ValidateRemittanceDocument(It.IsAny<RemittanceNetworkEnrollmentTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DocGenParamInternal CreateGenParamInternal(string enrollType)
    {
        return new DocGenParamInternal()
        {
            SavePath = "/tmp/Result/",
            DocGenParam = new DocGenParam()
            {
                StatusRecord = new DocSys.Common.StatusRecord()
                {
                    NFPYDT_ID = "0",
                    TPA = "ABC",
                    NETWORKNAME = "TEST",
                    NETWORKENROLLMENTTYPE = enrollType,
                    TIN = "9999999"
                }
            }
        };
    }

    [Theory]
    [MemberData(nameof(InvalidCtorParameters))]
    public void ShouldThrowExceptionOnInvalidCtorValues(Type exceptionType, IStpRemittanceRepository stpRemittanceRepository,
        ITrn835DataRepository trn835DataRepository, IFileSystem fileSystem, ILogger<StpRemittanceDocumentService> logger,
        IRemittancePartnerNetworkApiClient remittancePartnerNetworkApiClient)
    {
        Assert.Throws(exceptionType, () => new StpRemittanceDocumentService(stpRemittanceRepository, trn835DataRepository,
                                                fileSystem, logger, remittancePartnerNetworkApiClient));
    }

    public static IEnumerable<object[]> InvalidCtorParameters
    {
        get
        {
            var srr = Mock.Of<IStpRemittanceRepository>();
            var tdr = Mock.Of<ITrn835DataRepository>();
            var fs = Mock.Of<IFileSystem>();
            var log = new NullLogger<StpRemittanceDocumentService>();
            var rpc = Mock.Of<IRemittancePartnerNetworkApiClient>();

            yield return new object[] { typeof(ArgumentNullException), null, tdr, fs, log, rpc };
            yield return new object[] { typeof(ArgumentNullException), srr, null, fs, log, rpc };
            yield return new object[] { typeof(ArgumentNullException), srr, tdr, null, log, rpc };
            yield return new object[] { typeof(ArgumentNullException), srr, tdr, fs, null, rpc };
            yield return new object[] { typeof(ArgumentNullException), srr, tdr, fs, log, null };
        }
    }
}
