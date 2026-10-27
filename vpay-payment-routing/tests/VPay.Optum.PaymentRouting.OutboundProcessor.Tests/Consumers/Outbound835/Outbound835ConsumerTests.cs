using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdiFabric.Templates.Hipaa5010;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VPay.Extensions.Exceptions;
using VPay.Optum.Http.ConfigApi.Models;
using VPay.Optum.Http.ConfigApi.Queries;
using VPay.Optum.Http.ReconReportsApi.Models;
using VPay.Optum.Http.ReconReportsApi.Queries;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.Outbound835;
using VPay.Optum.PaymentRouting.OutboundProcessor.Helpers;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Tests.Consumers.Outbound835
{
    public class Outbound835ConsumerTests
    {
        private readonly PaymentRoutingTestingContainer _container;

        public Outbound835ConsumerTests() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddMock<ILogger<Outbound835Consumer>>()
                    .AddMock<IMediator>()
                    .AddMock<IUserInfo>()
                    .AddMock<IClobConverter>()
                    .AddSingleton<Outbound835Consumer>();
            });

        [Fact]
        public async Task ConsumeWithNoPaymentMessage_ThrowsException()
        {
            var provider = new ServiceCollection()
                .AddLogging(l => l.AddProvider(NullLoggerProvider.Instance))
                .AddMassTransitInMemoryTestHarness(cfg =>
                {
                    cfg.AddConsumer<Outbound835Consumer>();
                    cfg.AddConsumerTestHarness<Outbound835Consumer>();
                })
                .BuildServiceProvider(true);
            var harness = provider.GetRequiredService<InMemoryTestHarness>();
            try
            {
                await harness.Start();
                var bus = provider.GetRequiredService<IBus>();

                IRequestClient<BuildOutbound835> client = bus.CreateRequestClient<BuildOutbound835>();

                Func<Task> act = () => client.GetResponse<BuildOutbound835>(null);

                await act.Should().ThrowAsync<RequestException>();
            }
            finally
            {
                await harness.Stop();
                await provider.DisposeAsync();
            }
        }

        [Fact]
        public async Task ConsumeWithNullPaymentMessage_ThrowsException()
        {
            var consumer = _container.Get<Outbound835Consumer>();

            Func<Task> act = () => consumer.Consume(null);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ConsumePaymentWithNullPaymentMessage_ThrowsException()
        {
            var consumer = _container.Get<Outbound835Consumer>();

            Func<Task> act = () => consumer.ConsumePayment(null, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("123456789", null, null)]
        [InlineData("123456789", "123456789", null)]
        [InlineData("123456789", "123456789", "a@mail.com")]
        [InlineData("123456789", "a@mail.com", "123456789")]
        [InlineData("a@mail.com", "123456789", "123456789")]
        public async Task ConsumePayment_WithPaymentMessagePayerContacts_WorksAsExpected(string bl04, string bl06, string bl08)
        {
            var clobConverterMock = _container.Get<IClobConverter>();
            var fake835 = new TS835
            {
                AllN1 = new All_N1_835
                {
                    Loop1000A = new Loop_1000A_835
                    {
                        REF_AdditionalPayerIdentification = new List<REF_AdditionalPayerIdentification>
                        {
                            new()
                            {
                                ReferenceIdentificationQualifier_01 = "EO"
                            }
                        }
                    }
                }
            };

            clobConverterMock.ClobTo835(Arg.Any<string>()).ReturnsForAnyArgs(fake835);
            clobConverterMock.Ts835ToClob(Arg.Any<TS835>()).ReturnsForAnyArgs(_container.Fake<string>());

            var mediatorMock = _container.Get<IMediator>();
            mediatorMock.Send(Arg.Any<TransactionBankAccountsQuery>(), Arg.Any<CancellationToken>()).Returns(_container.Fake<TransactionBankAccounts>());

            var apMessageTracking = _container.Fake<ApMessageTracking>(x => x
                .With(amt => amt.Status, ApMessageTrackingStatus.ApSent)
                .With(amt => amt.TrnSegment, "fake0*fake1*fake2*fake3*fake4")
                .With(amt => amt.TransactionId, 12345)
                .With(amt => amt.PaymentTypeCode, "CLSDA"));
            mediatorMock.Send(Arg.Any<GetApMessageTrackingByIdSql.Query>())
                .ReturnsForAnyArgs(apMessageTracking);

            var fakeInboundPayment = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            mediatorMock.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>())
                .ReturnsForAnyArgs(fakeInboundPayment);

            var fakeOtbDetail = _container.Fake<OtbBillingEntityDetailDto>();
            var updatedValues = fakeOtbDetail.Fields.ToList();
            updatedValues.Add(new OtbBillingEntityFieldDto
            {
                FieldName = "PER_BL_04",
                FieldValue = bl04
            });
            updatedValues.Add(new OtbBillingEntityFieldDto
            {
                FieldName = "PER_BL_06",
                FieldValue = bl06
            });
            updatedValues.Add(new OtbBillingEntityFieldDto
            {
                FieldName = "PER_BL_08",
                FieldValue = bl08
            });
            fakeOtbDetail.Fields = updatedValues;
            mediatorMock.Send(Arg.Any<OtbBillingEntityDetailQuery>())
                .ReturnsForAnyArgs(fakeOtbDetail);

            var consumer = _container.Get<Outbound835Consumer>();

            Func<Task> act = () => consumer.ConsumePayment(new BuildOutbound835(apMessageTracking.ApMessageTrackingId, _container.Fake<string>()), CancellationToken.None);

            await act.Should().NotThrowAsync();
            await clobConverterMock.Received(1).ClobTo835(Arg.Any<string>());
            await clobConverterMock.Received(1).Ts835ToClob(Arg.Any<TS835>());
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("ABC01", null)]
        [InlineData(null, "123456789")]
        [InlineData("ABC01", "123456789")]
        public async Task ConsumePayment_WithPaymentMessagePayerReferences_WorksAsExpected(string payerId, string subPayerId)
        {
            var clobConverterMock = _container.Get<IClobConverter>();
            var clobConverter = new ClobConverter();

            var fakeTxt =
                "ST*835*182830992~" +
                "BPR*I*10*C*ACH*CCP*01*000*DA*000*1999999100*0000UMR01*01*000*DA*000*20220316~" +
                $"TRN*1*PAYNUM0123456789*{subPayerId ?? ""}*0000{payerId ?? ""}~" +
                "REF*EV*001122334~" +
                "DTM*405*20220316~" +
                "N1*PR*SOME CORPORATION~" +
                "N3*123 SOME AVE~" +
                "N4*SOMEWHERE*TX*75000~" +
                $"REF*2U*{payerId ?? ""}~" +
                $"REF*EO*{subPayerId ?? ""}~" +
                "PER*BL*SOME*EM*SOME-BUSINESS-EDI@SOME.COM~" +
                "N1*PE*SOME OTHER CORPORATION*XX*1234567890~" +
                "N3*456 OTHER ST~" +
                "N4*OTHER*TX*75000~" +
                "REF*TJ*3344556677~" +
                "LX*1~CLP*P1001001001*1*5*5**12*12341234567*49*1~";

            var fake835 = new TS835
            {
                TRN_ReassociationTraceNumber = new TRN_DependentTraceNumber
                {
                    TraceTypeCode_01 = "TRN",
                    CurrentTransactionTraceNumber_02 = "1",
                    OriginatingCompanyIdentifier_03 = $"{subPayerId ?? ""}",
                    ReferenceIdentification_04 = $"0000{payerId ?? ""}",
                },
                AllN1 = new All_N1_835
                {
                    Loop1000A = new Loop_1000A_835
                    {
                        REF_AdditionalPayerIdentification = new List<REF_AdditionalPayerIdentification>
                        {
                            new()
                            {
                                ReferenceIdentificationQualifier_01 = "2U",
                                MemberGrouporPolicyNumber_02 = payerId
                            },
                            new()
                            {
                                ReferenceIdentificationQualifier_01 = "EO",
                                MemberGrouporPolicyNumber_02 = subPayerId
                            }
                        }
                    }
                }
            };

            TS835 replacedRefs835 = null;
            TS835 converted835 = clobConverter.ClobTo835(fakeTxt).Result;

            clobConverterMock.ClobTo835(Arg.Any<string>())
                .ReturnsForAnyArgs(fake835);

            clobConverterMock.Ts835ToClob(Arg.Do<TS835>(p => replacedRefs835 = p))
                .ReturnsForAnyArgs(fakeTxt);

            var mediatorMock = _container.Get<IMediator>();
            mediatorMock.Send(Arg.Any<TransactionBankAccountsQuery>(), Arg.Any<CancellationToken>())
                .Returns(_container.Fake<TransactionBankAccounts>());

            var apMessageTracking = _container.Fake<ApMessageTracking>(x => x
                .With(amt => amt.Status, ApMessageTrackingStatus.ApSent)
                .With(amt => amt.TrnSegment, $"TRN*1*PAYNUM0123456789*{subPayerId}*0000{payerId}")
                .With(amt => amt.TransactionId, 12345)
                .With(amt => amt.PaymentTypeCode, "CLSDA"));
            mediatorMock.Send(Arg.Any<GetApMessageTrackingByIdSql.Query>())
                .ReturnsForAnyArgs(apMessageTracking);

            var fakeInboundPayment = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            mediatorMock.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>())
                .ReturnsForAnyArgs(fakeInboundPayment);

            var fakeOtbDetail = new OtbBillingEntityDetailDto
            {
                Fields = new List<OtbBillingEntityFieldDto>()
                {
                    new OtbBillingEntityFieldDto { FieldName = "BillingEntityName", FieldValue = "ABC" },
                    new OtbBillingEntityFieldDto { FieldName = "PayerAddressLine1", FieldValue = "111 VPAY AVE" },
                    new OtbBillingEntityFieldDto { FieldName = "PayerAddressLine2 ", FieldValue = "STE 222" },
                    new OtbBillingEntityFieldDto { FieldName = "PayerCityName", FieldValue = "VPAYOPOLIS" },
                    new OtbBillingEntityFieldDto { FieldName = "PayerStateCode", FieldValue = "TX" },
                    new OtbBillingEntityFieldDto { FieldName = "PayerZipCode", FieldValue = "75000-0000" },
                    new OtbBillingEntityFieldDto { FieldName = "PayerID", FieldValue = payerId },
                    new OtbBillingEntityFieldDto { FieldName = "PER_CX_02", FieldValue = "" },
                    new OtbBillingEntityFieldDto { FieldName = "PER_CX_03", FieldValue = "" },
                    new OtbBillingEntityFieldDto { FieldName = "PER_BL_02", FieldValue = "" },
                    new OtbBillingEntityFieldDto { FieldName = "PER_BL_03", FieldValue = "" },
                    new OtbBillingEntityFieldDto { FieldName = "PER_BL_04", FieldValue = "" },
                    new OtbBillingEntityFieldDto { FieldName = "PER_BL_05", FieldValue = "" },
                    new OtbBillingEntityFieldDto { FieldName = "PER_BL_06", FieldValue = "" },
                    new OtbBillingEntityFieldDto { FieldName = "PER_BL_07", FieldValue = "" },
                    new OtbBillingEntityFieldDto { FieldName = "PER_BL_08", FieldValue = "" }
                }
            };
            mediatorMock.Send(Arg.Any<OtbBillingEntityDetailQuery>())
                .ReturnsForAnyArgs(fakeOtbDetail);

            var consumer = _container.Get<Outbound835Consumer>();

            Func<Task> act = () => consumer.ConsumePayment(new BuildOutbound835(apMessageTracking.ApMessageTrackingId, fakeTxt), CancellationToken.None);

            await act.Should().NotThrowAsync();


            await clobConverterMock.ReceivedWithAnyArgs(1).ClobTo835(Arg.Any<string>());
            await clobConverterMock.ReceivedWithAnyArgs(1).Ts835ToClob(Arg.Any<TS835>());

            if (payerId == null)
            {
                // Check REF*2U is not included in Outbound 835 if PayerID is null or empty.
                replacedRefs835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification.Should().BeNull();
            }
            else
            {
                // Check REF*2U is included in Outbound 835 if PayerID has a valid value.
                replacedRefs835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification.Should().NotBeNull();
                replacedRefs835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification?.Count.Should().Be(1);

                replacedRefs835?.TRN_ReassociationTraceNumber?.OriginatingCompanyIdentifier_03.Should()
                    .Be(fake835?.TRN_ReassociationTraceNumber?.OriginatingCompanyIdentifier_03);
                replacedRefs835?.TRN_ReassociationTraceNumber?.ReferenceIdentification_04.Should()
                    .Be(fake835?.TRN_ReassociationTraceNumber?.ReferenceIdentification_04);
                replacedRefs835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification[0]?.ReferenceIdentificationQualifier_01.Should()
                    .Be(fake835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification[0]?.ReferenceIdentificationQualifier_01);
                replacedRefs835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification[0]?.MemberGrouporPolicyNumber_02.Should()
                    .Be(fake835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification[0]?.MemberGrouporPolicyNumber_02);

                // Check REF*2U value matches the PayerID in TRN04, without the leading zeros.
                replacedRefs835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification[0].MemberGrouporPolicyNumber_02.Should()
                    .Be(fake835?.TRN_ReassociationTraceNumber?.ReferenceIdentification_04.TrimStart('0'));

                // Check the original 835 contained both REFs for PayerId and SubPayerId
                converted835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification.Should().NotBeNull();
                converted835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification?.Count.Should().Be(2);
                converted835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification[0]?.ReferenceIdentificationQualifier_01.Should().Be("2U");
                converted835?.AllN1.Loop1000A.REF_AdditionalPayerIdentification[1]?.ReferenceIdentificationQualifier_01.Should().Be("EO");

                // Check N1/N3/N4 replaced payer name and address matches the values in the OTB file
                var fields = fakeOtbDetail.Fields.ToDictionary(x => x.FieldName, x => x.FieldValue);
                replacedRefs835?.AllN1.Loop1000A.N1_PayerIdentification.EntityIdentifierCode_01.Should()
                    .Be("PR");
                replacedRefs835?.AllN1.Loop1000A.N1_PayerIdentification.PremiumPayerName_02.Should()
                    .Be(fields.GetValueOrDefault("BillingEntityName"));
                replacedRefs835?.AllN1.Loop1000A.N3_PayerAddress.ResponseContactAddressLine_01.Should()
                    .Be(fields.GetValueOrDefault("PayerAddressLine1"));
                replacedRefs835?.AllN1.Loop1000A.N3_PayerAddress.ResponseContactAddressLine_02.Should()
                    .Be(fields.GetValueOrDefault("PayerAddressLine2 "));
                replacedRefs835?.AllN1.Loop1000A.N4_PayerCity_State_ZIPCode.AdditionalPatientInformationContactCityName_01.Should()
                    .Be(fields.GetValueOrDefault("PayerCityName"));
                replacedRefs835?.AllN1.Loop1000A.N4_PayerCity_State_ZIPCode.AdditionalPatientInformationContactStateCode_02.Should()
                    .Be(fields.GetValueOrDefault("PayerStateCode"));
                replacedRefs835?.AllN1.Loop1000A.N4_PayerCity_State_ZIPCode.AdditionalPatientInformationContactPostalZoneorZIPCode_03.Should()
                    .Be(fields.GetValueOrDefault("PayerZipCode")?.Replace("-", string.Empty));
            }
        }

        [Fact]
        public async Task ConsumePayment_WithPaymentMessageInApSentStatus_WorksAsExpected()
        {
            var clobConverterMock = _container.Get<IClobConverter>();
            var fake835 = CreateFake835();

            clobConverterMock.ClobTo835(Arg.Any<string>()).ReturnsForAnyArgs(fake835);
            clobConverterMock.Ts835ToClob(Arg.Any<TS835>()).ReturnsForAnyArgs(_container.Fake<string>());

            var mediatorMock = _container.Get<IMediator>();
            mediatorMock.Send(Arg.Any<TransactionBankAccountsQuery>(), Arg.Any<CancellationToken>()).Returns(_container.Fake<TransactionBankAccounts>());

            var apMessageTracking = _container.Fake<ApMessageTracking>(x => x
                .With(amt => amt.Status, ApMessageTrackingStatus.ApSent)
                .With(amt => amt.TrnSegment, "fake0*fake1*fake2*fake3*fake4")
                .With(amt => amt.TransactionId, 12345)
                .With(amt => amt.PaymentTypeCode, "CLSDA"));
            mediatorMock.Send(Arg.Any<GetApMessageTrackingByIdSql.Query>())
                .ReturnsForAnyArgs(apMessageTracking);

            var fakeInboundPayment = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            mediatorMock.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>())
                .ReturnsForAnyArgs(fakeInboundPayment);

            var fakeOtbDetail = _container.Fake<OtbBillingEntityDetailDto>();
            SetFakeOtbDetailFields(fakeOtbDetail);
            mediatorMock.Send(Arg.Any<OtbBillingEntityDetailQuery>())
                .ReturnsForAnyArgs(fakeOtbDetail);

            var consumer = _container.Get<Outbound835Consumer>();

            Func<Task> act = () => consumer.ConsumePayment(new BuildOutbound835(apMessageTracking.ApMessageTrackingId, _container.Fake<string>()), CancellationToken.None);

            await act.Should().NotThrowAsync();
            await clobConverterMock.Received(1).ClobTo835(Arg.Any<string>());
            await clobConverterMock.Received(1).Ts835ToClob(Arg.Any<TS835>());
        }

        [Fact]
        public async Task ConsumePayment_WithPaymentMessageInStartedStatus_WorksAsExpected()
        {
            var clobConverterMock = _container.Get<IClobConverter>();
            var fake835 = CreateFake835();

            clobConverterMock.ClobTo835(Arg.Any<string>()).ReturnsForAnyArgs(fake835);
            clobConverterMock.Ts835ToClob(Arg.Any<TS835>()).ReturnsForAnyArgs(_container.Fake<string>());

            var mediatorMock = _container.Get<IMediator>();
            mediatorMock.Send(Arg.Any<TransactionBankAccountsQuery>(), Arg.Any<CancellationToken>()).Returns(_container.Fake<TransactionBankAccounts>());

            var apMessageTracking = _container.Fake<ApMessageTracking>(x => x
                .With(amt => amt.Status, ApMessageTrackingStatus.Started)
                .With(amt => amt.TrnSegment, "fake0*fake1*fake2*fake3*fake4")
                .With(amt => amt.TransactionId, 12345)
                .With(amt => amt.PaymentTypeCode, "CLSDA"));
            mediatorMock.Send(Arg.Any<GetApMessageTrackingByIdSql.Query>())
                .ReturnsForAnyArgs(apMessageTracking);

            var fakeInboundPayment = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            mediatorMock.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>())
                .ReturnsForAnyArgs(fakeInboundPayment);

            var fakeOtbDetail = _container.Fake<OtbBillingEntityDetailDto>();
            SetFakeOtbDetailFields(fakeOtbDetail);
            mediatorMock.Send(Arg.Any<OtbBillingEntityDetailQuery>())
                .ReturnsForAnyArgs(fakeOtbDetail);

            var consumer = _container.Get<Outbound835Consumer>();

            Func<Task> act = () => consumer.ConsumePayment(new BuildOutbound835(apMessageTracking.ApMessageTrackingId, _container.Fake<string>()), CancellationToken.None);

            await act.Should().NotThrowAsync();
            await clobConverterMock.Received(1).ClobTo835(Arg.Any<string>());
            await clobConverterMock.Received(1).Ts835ToClob(Arg.Any<TS835>());
        }

        [Fact]
        public async Task ConsumePayment_WithPaymentMessageInCompletedStatus_SkipsProcessing()
        {
            var fake835 = CreateFake835();

            var mediatorMock = _container.Get<IMediator>();

            var apMessageTracking = _container.Fake<ApMessageTracking>(x => x
                .With(amt => amt.Status, ApMessageTrackingStatus.Completed)
                .With(amt => amt.TrnSegment, "fake0*fake1*fake2*fake3*fake4")
                .With(amt => amt.TransactionId, 12345)
                .With(amt => amt.PaymentTypeCode, "CLSDA"));
            mediatorMock.Send(Arg.Any<GetApMessageTrackingByIdSql.Query>())
                .ReturnsForAnyArgs(apMessageTracking);

            var fakeInboundPayment = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            mediatorMock.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>())
                .ReturnsForAnyArgs(fakeInboundPayment);

            var consumer = _container.Get<Outbound835Consumer>();

            Func<Task> act = () => consumer.ConsumePayment(new BuildOutbound835(apMessageTracking.ApMessageTrackingId, _container.Fake<string>()), CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ConsumePayment_WithPaymentMessageInOtherStatus_ThrowsBadRequestException()
        {
            var fake835 = CreateFake835();

            var mediatorMock = _container.Get<IMediator>();

            var apMessageTracking = _container.Fake<ApMessageTracking>(x => x
                .With(amt => amt.Status, ApMessageTrackingStatus.Queued)
                .With(amt => amt.TrnSegment, "fake0*fake1*fake2*fake3*fake4")
                .With(amt => amt.TransactionId, 12345)
                .With(amt => amt.PaymentTypeCode, "CLSDA"));
            mediatorMock.Send(Arg.Any<GetApMessageTrackingByIdSql.Query>())
                .ReturnsForAnyArgs(apMessageTracking);

            var fakeInboundPayment = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            mediatorMock.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>())
                .ReturnsForAnyArgs(fakeInboundPayment);

            var consumer = _container.Get<Outbound835Consumer>();

            Func<Task> act = () => consumer.ConsumePayment(new BuildOutbound835(apMessageTracking.ApMessageTrackingId, _container.Fake<string>()), CancellationToken.None);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        private static TS835 CreateFake835()
        {
            var fake835 = new TS835
            {
                AllN1 = new All_N1_835
                {
                    Loop1000A = new Loop_1000A_835
                    {
                        REF_AdditionalPayerIdentification = new List<REF_AdditionalPayerIdentification>
                        {
                            new()
                            {
                                ReferenceIdentificationQualifier_01 = "EO"
                            }
                        }
                    }
                }
            };

            return fake835;
        }

        private static void SetFakeOtbDetailFields(OtbBillingEntityDetailDto fakeOtbDetail)
        {
            var updatedValues = fakeOtbDetail.Fields.ToList();
            updatedValues.Add(new OtbBillingEntityFieldDto
            {
                FieldName = "PER_BL_04",
                FieldValue = null
            });
            updatedValues.Add(new OtbBillingEntityFieldDto
            {
                FieldName = "PER_BL_06",
                FieldValue = null
            });
            updatedValues.Add(new OtbBillingEntityFieldDto
            {
                FieldName = "PER_BL_08",
                FieldValue = null
            });
            fakeOtbDetail.Fields = updatedValues;
        }
    }
}
