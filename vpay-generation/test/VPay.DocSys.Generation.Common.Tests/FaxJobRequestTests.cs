using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using VPay.DocSys.Common;

using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class FaxJobRequestTests
    {
#pragma warning disable IDE1006 // Naming Styles
        private FaxJobRequest SUT;
#pragma warning restore IDE1006 // Naming Styles

        [Theory]
        [MemberData(nameof(FaxReserveNameForFaxSystem))]
        public void ShouldSetValidPropertiesFromTiffQueueMessageFromCtor(FaxReserveName before, FaxReserveName after)
        {
            var tqm = new TiffQueueMessage
            {
                FaxNumber = "fax number",
                BillingEntity = "billing entity",
                Origin = "origin",
                PayType = "pay type",
                PrintVendor = "print vendor",
                Tpa = "tpa",
                Priority = 10,
                BoardUploadTimeStamp = DateTime.Now,
                ReserveName = before,
                TransactionIds = new List<long> { 0L }
            };

            SUT = new FaxJobRequest(tqm, 1, 1, new FaxJobSettings { Scale = 1, MaxHoursCap = 1 });

            Assert.Equal(tqm.FaxNumber, SUT.PhoneNumber);
            Assert.Equal(tqm.BillingEntity, SUT.BillingEntity);
            Assert.Equal(tqm.PayType, SUT.PayType);
            Assert.Equal(tqm.Tpa, SUT.Tpa);
            Assert.Equal(tqm.Priority, SUT.Priority);
            Assert.Equal(tqm.PrintVendor, SUT.PrintVendor);
            Assert.Equal(tqm.Origin, SUT.Origin);
            Assert.Equal(tqm.BoardUploadTimeStamp, SUT.BoardUploadDateTime);
            Assert.Equal(after.ToString(), SUT.ReserveName.ToUpper(CultureInfo.InvariantCulture));

            Assert.Null(SUT.DocumentPath);
            Assert.Equal("Fax", SUT.CorrespondenceMethod);
            Assert.Equal("F", SUT.SendInstruction);

            Assert.Equal(4, SUT.MaxRetries);
            Assert.Equal(0, SUT.CorrespondenceId);
            Assert.Equal(0, SUT.TransactionIds.First());

            Assert.Equal(1, SUT.PageCount);
            Assert.Equal(1U, SUT.FileSize);
        }

        [Fact]
        public void ShouldThrowExceptionForInvalidFaxJobSettings()
        {
            TiffQueueMessage tqm = new TiffQueueMessage() { TransactionIds = new List<long> { 0L } };
            FaxJobSettings fjs = new FaxJobSettings();

            Assert.Throws<ArgumentNullException>(() => new FaxJobRequest(null, 1, 1U, fjs));

            Assert.Throws<ArgumentNullException>(() => new FaxJobRequest(tqm, 1, 1U, null));

            Assert.Throws<ArgumentException>(() => new FaxJobRequest(tqm, 1, 1U, fjs));

            fjs.Scale = 1;
            Assert.Throws<ArgumentException>(() => new FaxJobRequest(tqm, 1, 1U, fjs));

            fjs.MaxHoursCap = 1;
            var actual = new FaxJobRequest(tqm, 1, 1U, fjs);
            Assert.NotNull(actual);
        }

        [Theory]
        [MemberData(nameof(MaxCapTests))]
        [MemberData(nameof(WarrantyTests))]
        [MemberData(nameof(ScaleTests))]
        public void ShouldCalculateAbandonAfterCorrectly(FaxReserveName reserveName, int pageCount, double scale, int padding, int maxHours, int expected)
        {
            ulong size = (ulong)pageCount * 72000;

            var faxJobSettings = new FaxJobSettings
            {
                PaddingInSeconds = padding,
                Scale = scale,
                MaxHoursCap = maxHours
            };

            SUT = new FaxJobRequest(new TiffQueueMessage { ReserveName = reserveName, TransactionIds = new List<long> { 1, 2, 3 } }, pageCount, size, faxJobSettings);

            Assert.Equal(expected, SUT.AbandonAfterMinutes);
        }

        /// <summary>
        /// <see cref="FaxJobSettings.MaxHoursCap"/> value is used unless it is greater than actual
        /// </summary>
        public static IEnumerable<object[]> MaxCapTests
        {
            get
            {
                foreach (var frn in FaxReserveNames)
                {
                    if (frn == FaxReserveName.WARRANTY) { continue; }

                    yield return new object[] { frn, 21, 1, 5, 1, 1 * 60 };
                    yield return new object[] { frn, 21, 1, 5, 2, 2 * 60 };
                    yield return new object[] { frn, 21, 1, 5, 3, 3 * 60 };
                    yield return new object[] { frn, 21, 1, 5, 4, 4 * 60 };
                    yield return new object[] { frn, 21, 1, 5, 5, 5 * 60 };
                    // instead of 360
                    yield return new object[] { frn, 21, 1, 5, 6, 335 };
                }
            }
        }

        /// <summary>
        /// <see cref="FaxReserveName.WARRANTY"/> is always 5 minutes, overrides <see cref="FaxJobSettings.PaddingInSeconds"/>
        /// </summary>
        public static IEnumerable<object[]> WarrantyTests
        {
            get
            {
                // warranty, always 5, overrides max cap hours
                yield return new object[] { FaxReserveName.WARRANTY, 1, 1, 0, 1, 5 };
                yield return new object[] { FaxReserveName.WARRANTY, 1, 1, 31, 1, 5 };
                yield return new object[] { FaxReserveName.WARRANTY, 16, 1, 0, 1, 5 };
                yield return new object[] { FaxReserveName.WARRANTY, 16, 2, 0, 1, 5 };
            }
        }

        /// <summary>
        /// calculation based on size and <see cref="FaxJobSettings.PaddingInSeconds"/> and <see cref="FaxJobSettings.Scale"/>
        /// </summary>
        public static IEnumerable<object[]> ScaleTests
        {
            get
            {
                foreach (var frn in FaxReserveNames)
                {
                    if (frn == FaxReserveName.WARRANTY) { continue; }
                    // scale by 1, 1 second padding
                    yield return new object[] { frn, 1, 1, 0, 10, 30 };

                    // scale by 1, 5 seconds padding
                    yield return new object[] { frn, 100, 1, 5, 30, 1520 };
                    yield return new object[] { frn, 5, 1, 5, 30, 95 };

                    // scale by 2, 30 seconds padding
                    yield return new object[] { frn, 1, 2, 30, 20, 50 };
                    yield return new object[] { frn, 5, 2, 30, 20, 170 };
                    yield return new object[] { frn, 15, 2, 30, 20, 470 };
                    yield return new object[] { frn, 16, 2, 30, 20, 500 };

                    // scale by 2.5, 61 seconds padding
                    yield return new object[] { frn, 15, 2.5, 61, 20, 580 };
                    yield return new object[] { frn, 16, 2.5, 61, 20, 620 };
                }
            }
        }

        public static IEnumerable<object[]> FaxReserveNameForFaxSystem
        {
            get
            {
                foreach (var frn in FaxReserveNames)
                {
                    yield return new object[] { frn, frn == FaxReserveName.BFAX ? FaxReserveName.CSS : frn };
                }
            }
        }

        public static IEnumerable<FaxReserveName> FaxReserveNames
        {
            get
            {
                return Enum.GetValues(typeof(FaxReserveName)).Cast<FaxReserveName>();
            }
        }
    }
}
