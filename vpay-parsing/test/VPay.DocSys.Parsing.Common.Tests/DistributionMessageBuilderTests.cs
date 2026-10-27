using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Bogus;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common.Interfaces;

using Xunit;

namespace VPay.DocSys.Parsing.Common.Tests
{
    public class DistributionMessageBuilderTests
    {
        private readonly Mock<IEdi835Repository> _edi835Repository = new Mock<IEdi835Repository>();
        private readonly Mock<IServiceProvider> _serviceProvider = new Mock<IServiceProvider>();
        private readonly Mock<IServiceScope> _serviceScope = new Mock<IServiceScope>();
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory = new Mock<IServiceScopeFactory>();

        private readonly IDistributionMessageBuilder SUT;


        private static Faker Faker = new Faker();

        public DistributionMessageBuilderTests()
        {
            _edi835Repository.Setup(x => x.GetEdi835Configurations(CancellationToken.None)).ReturnsAsync(Edi835ConfigData());
            _serviceProvider.Setup(x => x.GetService(typeof(IEdi835Repository))).Returns(_edi835Repository.Object);
            _serviceScope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);
            _serviceScopeFactory.Setup(x => x.CreateScope()).Returns(_serviceScope.Object);

            SUT = new DistributionMessageBuilder(new Edi835ConfigurationCache(_serviceScopeFactory.Object), NullLogger<DistributionMessageBuilder>.Instance);
        }

#pragma warning disable CA1034 // Nested types should not be visible
        public class WhenCreatingMessage : DistributionMessageBuilderTests
#pragma warning restore CA1034 // Nested types should not be visible
        {
            [Theory]
            [MemberData(nameof(Arguments))]
            public void ShouldSetIsCustomSendBackTpaCorrectly(bool setFileUploadTimeStamp, bool isCustomSendBackTpa, bool hasSendBackRecords, bool hasFaxRecords, bool hasNonFaxRecords, bool isSwk)
            {
                var statusFile = CreateTestData(setFileUploadTimeStamp, hasSendBackRecords, hasFaxRecords, hasNonFaxRecords, isSwk);

                var actual = SUT.CreateDistributionMessage(statusFile, "TEST", 0, isCustomSendBackTpa, 1);

                Assert.Equal(isCustomSendBackTpa, actual.IsCustomSendBackTpa);
            }

            [Theory]
            [MemberData(nameof(Arguments))]
            public void ShouldSetPriorityCorrectly(bool setFileUploadTimeStamp, bool isCustomSendBackTpa, bool hasSendBackRecords, bool hasFaxRecords, bool hasNonFaxRecords, bool isSwk)
            {
                var statusFile = CreateTestData(setFileUploadTimeStamp, hasSendBackRecords, hasFaxRecords, hasNonFaxRecords, isSwk);

                var actual = SUT.CreateDistributionMessage(statusFile, "TEST", 0, isCustomSendBackTpa, 1);

                Assert.True(isSwk
                    ? actual.Transactions.TrueForAll(_ => _.Priority == 25)
                    : actual.Transactions.TrueForAll(_ => _.Priority == 50));
            }

            [Theory]
            [MemberData(nameof(Arguments))]
            public void ShouldSetFaxCompleteCorrectly(bool setFileUploadTimeStamp, bool isCustomSendBackTpa, bool hasSendBackRecords, bool hasFaxRecords, bool hasNonFaxRecords, bool isSwk)
            {
                var statusFile = CreateTestData(setFileUploadTimeStamp, hasSendBackRecords, hasFaxRecords, hasNonFaxRecords, isSwk);

                var actual = SUT.CreateDistributionMessage(statusFile, "TEST", 0, isCustomSendBackTpa, 1);

                Assert.Equal(!hasFaxRecords, actual.FaxComplete);
            }

            [Theory]
            [MemberData(nameof(Arguments))]
            public void ShouldSetNonFaxCompleteCorrectly(bool setFileUploadTimeStamp, bool isCustomSendBackTpa, bool hasSendBackRecords, bool hasFaxRecords, bool hasNonFaxRecords, bool isSwk)
            {
                var statusFile = CreateTestData(setFileUploadTimeStamp, hasSendBackRecords, hasFaxRecords, hasNonFaxRecords, isSwk);

                var actual = SUT.CreateDistributionMessage(statusFile, "TEST", 0, isCustomSendBackTpa, 1);

                Assert.Equal(!hasNonFaxRecords && (isCustomSendBackTpa || !hasSendBackRecords), actual.NonFaxComplete);
            }

            [Theory]
            [MemberData(nameof(Arguments))]
            public void ShouldSetClientPaymentIdCorrectly(bool setFileUploadTimeStamp, bool isCustomSendBackTpa, bool hasSendBackRecords, bool hasFaxRecords, bool hasNonFaxRecords, bool isSwk)
            {
                var statusFile = CreateTestData(setFileUploadTimeStamp, hasSendBackRecords, hasFaxRecords, hasNonFaxRecords, isSwk);

                var actual = SUT.CreateDistributionMessage(statusFile, "TEST", 0, isCustomSendBackTpa, 1);

                var records = statusFile.Records.ToDictionary(k => long.Parse(k.TXID.RemoveLeadingZeroes()), v => v);

                foreach (var a in actual.Transactions)
                {
                    a.ClientPaymentId.Should().NotBeNull();
                    a.ClientPaymentId.Should().BeSameAs(records[a.TxnId].USERKEY);
                }
            }

            [Theory]
            [MemberData(nameof(Arguments))]
            public void ShouldSetWarningTimeCorrectly(bool setFileUploadTimeStamp, bool isCustomSendBackTpa, bool hasSendBackRecords, bool hasFaxRecords, bool hasNonFaxRecords, bool isSwk)
            {
                var statusFile = CreateTestData(setFileUploadTimeStamp, hasSendBackRecords, hasFaxRecords, hasNonFaxRecords, isSwk);

                var warningTime = 1;
                var actual = SUT.CreateDistributionMessage(statusFile, "TEST", 0, isCustomSendBackTpa, warningTime);

                Assert.Equal(warningTime, actual.WarningTime.Subtract(actual.CreateDate).Hours);
            }

            [Theory]
            [MemberData(nameof(Arguments))]
            public void ShouldUseCorrectDateTimeValue(bool setFileUploadTimeStamp, bool isCustomSendBackTpa,
                bool hasSendBackRecords, bool hasFaxRecords, bool hasNonFaxRecords, bool isSwk)
            {
                var statusFile = CreateTestData(setFileUploadTimeStamp, hasSendBackRecords, hasFaxRecords,
                    hasNonFaxRecords, isSwk);

                var actual = SUT.CreateDistributionMessage(statusFile, "TEST", 0, isCustomSendBackTpa, 1);

                Assert.Equal(actual.BoardUploadTimeStamp, setFileUploadTimeStamp
                        ? statusFile.Header.FileUploadTimeStamp
                        : statusFile.Header.RecordDate);
            }

            [Theory]
            [MemberData(nameof(InvalidArguments))]
            public void ShouldThrowExceptionForNoRecords(bool setFileUploadTimeStamp, bool isCustomSendBackTpa, bool hasSendBackRecords, bool hasFaxRecords, bool hasNonFaxRecords, bool isSwk)
            {
                var statusFile = CreateTestData(setFileUploadTimeStamp, hasSendBackRecords, hasFaxRecords,
                   hasNonFaxRecords, isSwk);

                Assert.Throws<ArgumentException>(() => SUT.CreateDistributionMessage(statusFile, "TEST", 0, isCustomSendBackTpa, 1));
            }

            [Theory]
            [MemberData(nameof(Arguments))]
            public void ShouldThrowExceptionForEmptyPath(bool setFileUploadTimeStamp, bool isCustomSendBackTpa, bool hasSendBackRecords, bool hasFaxRecords, bool hasNonFaxRecords, bool isSwk)
            {
                var statusFile = CreateTestData(setFileUploadTimeStamp, hasSendBackRecords, hasFaxRecords,
                   hasNonFaxRecords, isSwk);

                Assert.Throws<ArgumentException>(() => SUT.CreateDistributionMessage(statusFile, "", 0, isCustomSendBackTpa, 1));
            }

            [Theory]
            [MemberData(nameof(Arguments))]
            public void ShouldThrowExceptionForNullPath(bool setFileUploadTimeStamp, bool isCustomSendBackTpa, bool hasSendBackRecords, bool hasFaxRecords, bool hasNonFaxRecords, bool isSwk)
            {
                var statusFile = CreateTestData(setFileUploadTimeStamp, hasSendBackRecords, hasFaxRecords,
                   hasNonFaxRecords, isSwk);

                Assert.Throws<ArgumentNullException>(() => SUT.CreateDistributionMessage(statusFile, null, 0, isCustomSendBackTpa, 1));
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void ShouldThrowExceptionForNullStatusFile(bool isCustomSendBackTpa)
            {
                Assert.Throws<ArgumentNullException>(() => SUT.CreateDistributionMessage(null, "TEST", 0, isCustomSendBackTpa, 1));
            }
        }

        /// <summary>
        /// setFileUploadTimeStamp, isCustomSendBackTpa, hasSendBackRecords, hasFaxRecords, hasNonFaxRecords, isSwk
        /// </summary>
        public static IEnumerable<object[]> Arguments => new List<object[]>
        {
            new object[] {true, true, true, true, true, true},
            new object[] {true, true, true, true, true, false},
            new object[] {false, true, true, true, true, true},
            new object[] {false, true, true, true, true, false},

            new object[] {true, true, true, true, false, true},
            new object[] {true, true, true, true, false, false},
            new object[] {false, true, true, true, false, true},
            new object[] {false, true, true, true, false, false},

            new object[] {true, true, true, false, true, true},
            new object[] {true, true, true, false, true, false},
            new object[] {true, true, true, false, false, true},
            new object[] {true, true, true, false, false, false},
            new object[] {false, true, true, false, true, true},
            new object[] {false, true, true, false, true, false},
            new object[] {false, true, true, false, false, true},
            new object[] {false, true, true, false, false, false},

            new object[] {true, true, false, true, true, true},
            new object[] {true, true, false, true, true, false},
            new object[] {true, true, false, true, false, true},
            new object[] {true, true, false, true, false, false},
            new object[] {true, true, false, false, true, true},
            new object[] {true, true, false, false, true, false},
            new object[] {false, true, false, true, true, true},
            new object[] {false, true, false, true, true, false},
            new object[] {false, true, false, true, false, true},
            new object[] {false, true, false, true, false, false},
            new object[] {false, true, false, false, true, true},
            new object[] {false, true, false, false, true, false},

            new object[] {true, false, true, true, true, true},
            new object[] {true, false, true, true, true, false},
            new object[] {true, false, true, true, false, true},
            new object[] {true, false, true, true, false, false},
            new object[] {true, false, true, false, true, true},
            new object[] {true, false, true, false, true, false},
            new object[] {true, false, true, false, false, true},
            new object[] {true, false, true, false, false, false},
            new object[] {true, false, false, true, true, true},
            new object[] {true, false, false, true, true, false},
            new object[] {true, false, false, true, false, true},
            new object[] {true, false, false, true, false, false},
            new object[] {true, false, false, false, true, true},
            new object[] {true, false, false, false, true, false},
            new object[] {false, false, true, true, true, true},
            new object[] {false, false, true, true, true, false},
            new object[] {false, false, true, true, false, true},
            new object[] {false, false, true, true, false, false},
            new object[] {false, false, true, false, true, true},
            new object[] {false, false, true, false, true, false},
            new object[] {false, false, true, false, false, true},
            new object[] {false, false, true, false, false, false},
            new object[] {false, false, false, true, true, true},
            new object[] {false, false, false, true, true, false},
            new object[] {false, false, false, true, false, true},
            new object[] {false, false, false, true, false, false},
            new object[] {false, false, false, false, true, true},
            new object[] {false, false, false, false, true, false},
        };

        /// <summary>
        /// setFileUploadTimeStamp, isCustomSendBackTpa, hasSendBackRecords, hasFaxRecords, hasNonFaxRecords, isSwk
        /// the following are all false: hasSendBackRecords, hasFaxRecords, hasNonFaxRecords
        /// </summary>
        public static IEnumerable<object[]> InvalidArguments => new List<object[]>
        {
            new object[] {true, true, false, false, false, true},
            new object[] {true, true, false, false, false, false},
            new object[] {true, false, false, false, false, true},
            new object[] {true, false, false, false, false, false},

            new object[] {false, true, false, false, false, true},
            new object[] {false, true, false, false, false, false},
            new object[] {false, false, false, false, false, true},
            new object[] {false, false, false, false, false, false},
        };

        private static StatusFile CreateTestData(bool setFileUploadTimeStamp, bool hasSendBackRecords, bool hasFaxRecords, bool hasNonFaxRecords, bool isSwk)
        {
            var random = new Random();
            var tpa = isSwk ? "SWK" : "TEST";
            var date = DateTime.Now;

            var statusFile = new StatusFile
            {
                Header = new StatusHeader
                {
                    RecordDate = date,
                    FileUploadTimeStamp = setFileUploadTimeStamp ? date.AddDays(1) : null as DateTime?
                }
            };

            if (hasSendBackRecords)
            {
                statusFile.Records.Add(new StatusRecord
                {
                    TPA = tpa,
                    DIST = DistType.SendBack,
                    TXID = random.Next().ToString("D20"),
                    DESC = "HAS SEND BACK",
                    USERKEY = Faker.Random.String2(1, 100)
                }); ;
            }

            if (hasFaxRecords)
            {
                statusFile.Records.AddRange(new[]
                {
                    new StatusRecord
                    {
                        TPA = tpa,
                        DIST = DistType.Fax,
                        TXID = random.Next().ToString("D20"),
                        DESC = "HAS FAX - FAX",
                        USERKEY = Faker.Random.String2(1, 100),
                    },
                    new StatusRecord
                    {
                        TPA = tpa,
                        DIST = "R",
                        TXID = random.Next().ToString("D20"),
                        DESC = "HAS FAX - R",
                        USERKEY = Faker.Random.String2(1, 100),
                    }
                });
            }

            if (hasNonFaxRecords)
            {
                statusFile.Records.AddRange(new[]
                {
                    new StatusRecord
                    {
                        TPA = tpa,
                        DIST = DistType.Mail,
                        TXID = random.Next().ToString("D20"),
                        DESC = "HAS NON-FAX - MAIL",
                        USERKEY = Faker.Random.String2(1, 100),
                    }
                });
            }

            return statusFile;
        }

        private IList<Edi835Configuration> Edi835ConfigData()
        {
            return new List<Edi835Configuration>
            {
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 1,
                    ClientCode = "SWK",
                    Parser = "SWK",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 2,
                    ClientCode = "HEQTN",
                    Parser = "HEQTN",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 3,
                    ClientCode = "RC",
                    Parser = "RC",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 4,
                    ClientCode = "EHI",
                    Parser = "DirectSpec",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 4,
                    ClientCode = "HPS",
                    Parser = "DualSpec",
                }
            };
        }
    }
}
