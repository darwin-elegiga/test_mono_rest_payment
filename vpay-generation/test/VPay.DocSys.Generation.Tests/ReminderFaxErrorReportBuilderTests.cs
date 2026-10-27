using Bogus;

using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Collections.Generic;

using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class ReminderFaxErrorReportBuilderTests
    {
        private static readonly Faker Faker = new Faker();

        private readonly IReminderFaxErrorReportBuilder SUT = new ReminderFaxErrorReportBuilder(
            new NullLogger<ReminderFaxErrorReportBuilder>());

        [Theory]
        [MemberData(nameof(EmptyDictionaries))]
        public void ShouldReturnEmptyStringWhenNoReminderFaxesInDictionary(
            Dictionary<ReminderFaxErrorType, IList<ReminderFaxError>> dictionary)
        {
            var result = SUT.BuildReport(dictionary, 0);

            Assert.True(string.IsNullOrEmpty(result));
        }

        [Theory]
        [MemberData(nameof(ReminderFaxErrorTypes), true)]
        [MemberData(nameof(ReminderFaxErrorTypes), false)]
        public void ShouldCreateOneSectionForOneErrorType(ReminderFaxErrorType reminderFaxErrorType, Exception exception)
        {
            var actual = SUT.BuildReport(new Dictionary<ReminderFaxErrorType, IList<ReminderFaxError>>
            {
                { reminderFaxErrorType, new List<ReminderFaxError>{ new ReminderFaxError(new ReminderFax(), exception) } }
            }, 0);

            Assert.Contains(
                $"The following 1 Reminder Faxes could not be sent due to '{reminderFaxErrorType.GetDescription()}':",
                actual);

            Assert.Contains($">0</td><td>{exception?.Message}</td></tr>", actual);
        }

        public static IEnumerable<object[]> EmptyDictionaries
        {
            get
            {
                yield return new object[] { new Dictionary<ReminderFaxErrorType, IList<ReminderFaxError>>() };

                foreach (ReminderFaxErrorType item in Enum.GetValues(typeof(ReminderFaxErrorType)))
                {
                    yield return new object[] { new Dictionary<ReminderFaxErrorType, IList<ReminderFaxError>>
                        { { item, Array.Empty<ReminderFaxError>() } } };
                }
            }
        }

        public static IEnumerable<object[]> ReminderFaxErrorTypes(bool hasException)
        {
            foreach (ReminderFaxErrorType item in Enum.GetValues(typeof(ReminderFaxErrorType)))
            {
                yield return new object[] { item, hasException ? new Exception(Faker.Random.String2(10)) : null };
            }
        }
    }
}
