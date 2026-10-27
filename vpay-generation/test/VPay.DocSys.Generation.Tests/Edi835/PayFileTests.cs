using FluentAssertions;
using Moq;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VPay.Data.Db2.Abstractions.NonFundedPayment;
using VPay.Data.Db2.Abstractions.NonFundedPayment.Models;
using VPay.DocSys.Generation.Edi835.Application;
using VPay.DocSys.Generation.Edi835.Application.Models;
using VPay.DocSys.Generation.Edi835.Application.PayFile;
using Xunit;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835.ApplicationTests
{
    public class PayFileTests
    {
        private readonly Mock<INonFundedPaymentDetailRepository> _repositoryMock;
        private readonly Mock<IReconReportsApi> _apiMock;
        private readonly Mock<ILoafApi> _loafApiMock;

        private readonly PayfileService _sut;

        public PayFileTests()
        {
            _repositoryMock = new Mock<INonFundedPaymentDetailRepository>(MockBehavior.Strict);
            _apiMock = new Mock<IReconReportsApi>(MockBehavior.Strict);
            _loafApiMock = new Mock<ILoafApi>(MockBehavior.Strict);

            _sut = new PayfileService(_repositoryMock.Object, _apiMock.Object, _loafApiMock.Object, new NullLogger<PayfileService>());
        }

        #region CreateContent
        [Fact]
        public async Task CreateContent_WhenPassedInRecordIsNull_ThenShouldThrowNullArgumentException()
        {
            Func<Task<string>> method = async () => await _sut.GeneratePayFileDocument(null, default);

            await method.Should().ThrowAsync<ArgumentNullException>();
        }
        [Fact]
        public async Task CreateContent_WhenRecordIsPassedInIt_ThenItWillLookupThePayInfoFromRepositoryAndReturnStringWithThatData()
        {
            var record = GetCreationMessage(false);
            var payInfo = GetPayFileInfo();

            var expectedResult = BuildStringResult(record, payInfo, null, null);

            _repositoryMock.Setup(x => x.GetPayfileInfoById(12660, default)).ReturnsAsync(payInfo).Verifiable();

            var result = await _sut.GeneratePayFileDocument(record, default).ConfigureAwait(false);

            _repositoryMock.VerifyAll();

            result.Should().Be(expectedResult);
        }
        [Fact]
        public async Task CreateContent_WhenRecordIsPassedInIsEFT_ThenItWillCallApiToGetEftInfoAndReturnStringWithThatData()
        {
            var record = GetCreationMessage(true);
            var payInfo = GetPayFileInfo();
            var bankAccount = new BankAccount()
            {
                AccountNumber = "9999999999",
                RouteNumber = "123456"
            };

            var transactionInfo = new TransactionBankAccount()
            {
                AccountNumber = "1234599999",
                RouteNumber = "9865432"
            };

            var expectedResult = BuildStringResult(record, payInfo, bankAccount, transactionInfo);

            _repositoryMock.Setup(x => x.GetPayfileInfoById(12660, default)).ReturnsAsync(payInfo).Verifiable();
            _apiMock.Setup(x => x.GetMain835Account(default)).ReturnsAsync(bankAccount).Verifiable();
            _apiMock.Setup(x => x.GetBankAccountForTransactionId(1234, default)).ReturnsAsync(transactionInfo).Verifiable();

            var result = await _sut.GeneratePayFileDocument(record, default).ConfigureAwait(false);

            _apiMock.VerifyAll();

            result.Should().Be(expectedResult);
        }
        #endregion
        private DS.Edi835 GetCreationMessage(bool isEft)
        {
            return new DS.Edi835
            {
                NonFundedPaymentDetailId = 12660,
                TransactionId = 1234,
                OriginCode = "RC",
                ClientCode = "HSB",
                BillingEntityCode = "HSB0010001",
                ClaimId = "CLAIMID",
                PaymentTypeCode = isEft ? "CLEFT" : "TEMP",
                PaymentAmount = 1000.56m,
                LoadDate = new DateTime(2020, 10, 01),
                ClaimDate = new DateTime(2020, 10, 02),
                PayeeName = "CUSTOMER NAME",
                UserKey = "USERKEY0001"
            };
        }
        private string BuildStringResult(DS.Edi835 message, NonFundedPaymentDetailPayfileInfo info, BankAccount mainAcct, TransactionBankAccount trAcct)
        {
            mainAcct = mainAcct != null ? mainAcct : new BankAccount();
            trAcct = trAcct != null ? trAcct : new TransactionBankAccount();
            return $"{message.TransactionId.ToString().PadLeft(20,'0')}|{message.OriginCode,-20}|{message.ClientCode,-10}|{message.BillingEntityCode,-20}|{message.ClaimId,-20}|{info.UserDisplay3,-10}|{message.ClaimDate:yyyyMMdd}|{info.UserKey,-50}|{message.PaymentTypeCode,-5}|{(message.PaymentAmount * 100).ToString("F2").PadLeft(20, '0')}|   |{message.LoadDate:yyyyMMdd}|PORTUSER            |{info.TaxId,-20}|   |          |{message.PayeeName,-80}|                                                                                |{info.PayeeAddress1,-80}|{info.PayeeAddress2,-80}|{info.PayeeAddress3,-80}|{"",-80}|{info.PayeeCity,-30}|{info.PayeeState,-30}|{info.PayeeZipCode,-30}|{"",-50}|{mainAcct.RouteNumber,-9}|{mainAcct.AccountNumber,-17}|{trAcct.RouteNumber,-9}|{trAcct.AccountNumber,-17}|\n";
        }
        private NonFundedPaymentDetailPayfileInfo GetPayFileInfo()
        {
            return new NonFundedPaymentDetailPayfileInfo
            {
                Id = 12600,
                TransactionId = 1234,
                TaxId = "999998888",
                UserDisplay1 = "UDISPLAY1",
                UserDisplay2 = "UDISPLAY2",
                UserDisplay3 = "UDISPLAY3",
                UserKey = "USERKEY0001",
                PayeeAddress1 = "123 ADDRESS 1",
                PayeeAddress2 = "#12354",
                PayeeAddress3 = "ADDR3",
                PayeeCity = "CITY",
                PayeeState = "CO",
                PayeeZipCode = "75044"
            };
        }
    }
}
