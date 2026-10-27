using Newtonsoft.Json;

using System;

using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;

using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class ReminderFaxRequestTests
    {
        private readonly ReminderFaxRequest SUT;

        public ReminderFaxRequestTests()
        {
            SUT = new ReminderFaxRequest
            {
                BillingEntity = "TEST BE",
                ClientType = ClientType.MEDICAL,
                CorrespondenceId = 123L,
                UseFaxNumber = "TEST FAX",
                OriginCode = "TEST ORIGIN",
                LastFaxDate = DateTime.Now,
                PayeeName = "TEST PAYEE",
                AvailableBalance = 456.78m,
                ReminderType = ReminderFaxType.REMIND_BEG,
                TPACode = "TEST TPA",
                SeTransactionId = 91011L,
                PaymentTypeCode = PayType.CLVCP
            };
        }

        [Theory]
        [InlineData(ClientType.MEDICAL, ReminderFaxType.REMIND_BEG)]
        [InlineData(ClientType.MEDICAL, ReminderFaxType.REMIND_EXP)]
        [InlineData(ClientType.WARRANTY, ReminderFaxType.REMIND_BEG)]
        [InlineData(ClientType.WARRANTY, ReminderFaxType.REMIND_EXP)]
        public void ShouldCreateValidReminderFax(ClientType clientType, ReminderFaxType reminderFaxType)
        {
            SUT.ClientType = clientType;
            SUT.ReminderType = reminderFaxType;

            var actual = SUT.ToReminderFax();

            Assert.Null(actual.AddressLine1);
            Assert.Null(actual.AddressLine2);
            Assert.Null(actual.AddressLine3);
            Assert.Equal(0, actual.CurrentBalance);
            Assert.Equal(0, actual.ID);
            Assert.Equal(0, actual.LoadAmount);
            Assert.Null(actual.PayeeCity);
            Assert.Null(actual.PayeeState);
            Assert.Null(actual.PostalCode);
            Assert.Null(actual.ReqName);
            Assert.Equal(default, actual.TargetDate.Date);
            Assert.Null(actual.UserDisplay1);
            Assert.Null(actual.UserDisplay2);
            Assert.Null(actual.UserDisplay3);
            Assert.Null(actual.UserKey);
            Assert.Null(actual.VPaygeId);

            Assert.Equal(SUT.BillingEntity, actual.BillingEntity);
            Assert.Equal(SUT.ClientType, actual.ClientType);
            Assert.Equal(SUT.CorrespondenceId, actual.CorrespondenceId);
            Assert.Equal(SUT.UseFaxNumber, actual.UseFaxNumber);
            Assert.Equal(SUT.OriginCode, actual.OriginCode);
            Assert.Equal(SUT.LastFaxDate, actual.LastFaxDate);
            Assert.Equal(SUT.PayeeName, actual.PayeeName);
            Assert.Equal(PayType.CLVCP, actual.PaymentTypeCode);
            Assert.Equal(SUT.AvailableBalance, actual.AvailableBalance);
            Assert.Equal(SUT.ReminderType.ToString(), actual.ReminderType);
            Assert.Equal(SUT.TPACode, actual.TPACode);
            Assert.Equal(SUT.SeTransactionId, actual.SeTransactionId);
        }
    }
}
