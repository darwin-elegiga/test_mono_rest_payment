using System;
using VPay.Data.Db2.Abstractions.Fax;
using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class CoverSheetParametersTests
    {
        private CoverSheetParameters SUT;

        private readonly Random _random = new Random();

        private readonly string _vpayFaxNumber = "TEST VPAY FAX NUMBER";

        [Fact]
        public void ThrowExceptionOnInvalidCtorParameters()
        {
            Assert.Throws<ArgumentNullException>(() => new CoverSheetParameters(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new CoverSheetParameters(new ReminderFax(), null, null));
            Assert.Throws<ArgumentNullException>(() => new CoverSheetParameters(new ReminderFax(), new PayorDetail(), null));
            Assert.Throws<ArgumentNullException>(() => new CoverSheetParameters(new ReminderFax(), null, ""));
            Assert.Throws<ArgumentException>(() => new CoverSheetParameters(new ReminderFax(), new PayorDetail(), ""));
        }

        [Fact]
        public void PageCountIs0OnCtor()
        {
            SUT = new CoverSheetParameters();

            Assert.Equal(0, SUT.PageCount);

            SUT = new CoverSheetParameters(new ReminderFax(), new PayorDetail(), _vpayFaxNumber);

            Assert.Equal(0, SUT.PageCount);
        }

        [Fact]
        public void CurrentDateIsSetOnCtor()
        {
            SUT = new CoverSheetParameters();

            Assert.Equal(DateTime.Now.Date, SUT.CurrentDate.Date);

            SUT = new CoverSheetParameters(new ReminderFax(), new PayorDetail(), _vpayFaxNumber);

            Assert.Equal(DateTime.Now.Date, SUT.CurrentDate.Date);
        }

        [Theory]
        [InlineData(ReminderFaxType.REMIND_BEG)]
        [InlineData(ReminderFaxType.REMIND_EXP)]
        public void UsingCtorWithParametersSetsPropertiesCorrectly(ReminderFaxType reminderFaxType)
        {
            var reminderFax = new ReminderFax
            {
                LastFaxDate = DateTime.Now.AddDays(-_random.Next(1, 100)),
                AvailableBalance = (decimal)(_random.NextDouble() * 100),
                PayeeName = "TEST PAYEE NAME",
                SeTransactionId = (long)(_random.NextDouble() * 100000L),
                TPACode = "TEST TPA",
                UseFaxNumber = "TEST FAX NUMBER",
                ReminderType = reminderFaxType.ToString(),
            };

            var payeeDetail = new PayorDetail
            {
                PayorPhone = "TEST PHONE",
                PayorName = "TEST PAYEE",
                VPayPhone = "TEST VPAY PHONE"
            };

            SUT = new CoverSheetParameters(reminderFax, payeeDetail, _vpayFaxNumber);

            Assert.Equal(reminderFax.AvailableBalance, SUT.Balance);
            Assert.Equal(reminderFax.UseFaxNumber, SUT.FaxNumber);
            Assert.Equal(reminderFax.LastFaxDate, SUT.OriginalFaxDate);
            Assert.Equal(reminderFax.PayeeName, SUT.ProviderName);
            Assert.Equal(reminderFax.SeTransactionId, SUT.TransactionId);

            Assert.Equal(payeeDetail.PayorName, SUT.TpaName); ;
            Assert.Equal(payeeDetail.PayorPhone, SUT.TpaPhone);
            Assert.Equal(payeeDetail.VPayPhone, SUT.VPayPhone);

            Assert.Equal(_vpayFaxNumber, SUT.VPayFax);

            Assert.Equal(ReminderFaxType.REMIND_EXP.ToString() == reminderFax.ReminderType, SUT.IsFinal);
        }
    }
}
