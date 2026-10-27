using System;
using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class ReminderFaxEmailRequestTests
    {
        private ReminderFaxEmailSettings _settings;

        public ReminderFaxEmailRequestTests()
        {
            _settings = new ReminderFaxEmailSettings
            {
                FromEmail = "TEST",
                FromName = "TEST",
                ToEmail = "TEST",
                ToName = "TEST"
            };
        }

        [Fact]
        public void ShouldThrowExceptionOnInvalidValues()
        {
            _settings = null;

            Assert.Throws<ArgumentNullException>(() => new ReminderFaxEmailRequest(_settings, DateTime.Now, DateTime.Now));

            _settings = new ReminderFaxEmailSettings();
            Assert.Throws<ArgumentException>(() => new ReminderFaxEmailRequest(_settings, DateTime.Now, DateTime.Now));

            _settings.FromEmail = "";
            Assert.Throws<ArgumentException>(() => new ReminderFaxEmailRequest(_settings, DateTime.Now, DateTime.Now));

            _settings.FromEmail = "TEST";
            _settings.FromName = "";
            Assert.Throws<ArgumentException>(() => new ReminderFaxEmailRequest(_settings, DateTime.Now, DateTime.Now));

            _settings.FromName = "TEST";
            _settings.ToEmail = "";
            Assert.Throws<ArgumentException>(() => new ReminderFaxEmailRequest(_settings, DateTime.Now, DateTime.Now));

            _settings.ToEmail = "TEST";
            _settings.ToName = "";
            Assert.Throws<ArgumentException>(() => new ReminderFaxEmailRequest(_settings, DateTime.Now, DateTime.Now));

            _settings.ToName = "TEST";
            new ReminderFaxEmailRequest(_settings, DateTime.Now, DateTime.Now);
        }

        [Fact]
        public void ShouldShowBothDatesInSubjectWhenDifferent()
        {
            var start = DateTime.Now;
            var end = DateTime.Now.AddDays(1);
            var actual = new ReminderFaxEmailRequest(_settings, start, end);

            Assert.Equal($" {start:yyyy-MM-dd} to {end:yyyy-MM-dd} ", actual.EmailSubject.Substring(29, 26));
        }

        [Fact]
        public void ShouldShowOnlyOneDateInSubjectWhenSame()
        {

            var start = DateTime.Now;
            var actual = new ReminderFaxEmailRequest(_settings, start, start);

            Assert.Equal($" {start:yyyy-MM-dd} ", actual.EmailSubject.Substring(29, 12));
        }
    }
}
