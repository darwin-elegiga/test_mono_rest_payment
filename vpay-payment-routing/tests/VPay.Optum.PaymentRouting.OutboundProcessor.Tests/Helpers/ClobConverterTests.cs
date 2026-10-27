using System;
using System.Threading.Tasks;
using EdiFabric.Core.Model.Edi.X12;
using EdiFabric.Templates.Hipaa5010;
using FluentAssertions;
using VPay.Optum.PaymentRouting.OutboundProcessor.Helpers;
using Xunit;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Tests.Helpers
{
    public class ClobConverterFacts
    {
        [Fact]
        public async Task ClobTo835AndBack__WorksAsExpected()
        {
            var message = "ST*835*54411001~BPR*I*360*C*ACH*CCP*01*000*DA*000*1627395711*0000UMR01*01*000*DA*000*20170321~TRN*1*DPM1821000012507080000008*1627395711*0000UMR01~REF*EV*841162789~DTM*405*20170321~N1*PR*UMR CLARKSTON   POTOMAC GROUP INC~N3*115 W WAUSAU AVE~N4*WAUSAU*WI*54402~REF*2U*UMR01~REF*EO*1627395711~PER*BL*UMR EDI TEAM*EM*UMR-EDI-IT-TEAM_DL@DS.UHC.COM~N1*PE*DENTAL HEALTH ASSOCIATES PA*FI*222692491~LX*1~CLP*62032819*1*160*85*75*12*17076130021~NM1*QC*1*POTOMAC*CLARKSTON1~NM1*IL*1*POTOMAC*CLARKSTON1****MI*62032819~NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~REF*1L*76560010~AMT*AU*85~SVC*AD:D0120*75*0~DTM*472*20170313~CAS*PR*119*75~LQ*HE*N130~SVC*AD:D1110*85*85~DTM*472*20170313~AMT*B6*85~LX*2~CLP*63145082*1*160*85*75*12*17076130023~NM1*QC*1*YEAR*POLICY~NM1*IL*1*YEAR*POLICY****MI*63145082~NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~REF*1L*76560010~AMT*AU*85~SVC*AD:D0120*75*0~DTM*472*20170309~CAS*PR*119*75~LQ*HE*N130~SVC*AD:D1110*85*85~DTM*472*20170309~AMT*B6*85~LX*3~CLP*63145082*1*160*30*130*12*17076130024~NM1*QC*1*YEAR*SPOUSE~NM1*IL*1*YEAR*POLICY****MI*63145082~NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~REF*1L*76560010~AMT*AU*75~SVC*AD:D0120*75*30~DTM*472*20170313~CAS*PR*1*45~AMT*B6*75~SVC*AD:D1110*85*0~DTM*472*20170313~CAS*PR*119*85~LQ*HE*N130~LX*4~CLP*63145082*1*75*75**12*17076130025~NM1*QC*1*YEAR*CHILD2~NM1*IL*1*YEAR*POLICY****MI*63145082~NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~REF*1L*76560010~AMT*AU*75~SVC*AD:D0120*75*75~DTM*472*20170313~AMT*B6*75~LX*5~CLP*63145082*1*85*85**12*17076130026~NM1*QC*1*YEAR*CHILD2~NM1*IL*1*YEAR*POLICY****MI*63145082~NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~REF*1L*76560010~AMT*AU*85~SVC*AD:D0272*85*85~DTM*472*20170309~AMT*B6*85~SE*76*54411001~";
            var clobConverter = new ClobConverter();
            var ts835 = await clobConverter.ClobTo835(message);
            ts835.Should().NotBeNull();
            ts835.ST.Should().NotBeNull();
            ts835.ST.TransactionSetControlNumber_02.Should().Be("54411001");
            ts835.BPR_FinancialInformation.Should().NotBeNull();
            ts835.BPR_FinancialInformation.TransactionHandlingCode_01.Should().Be("I");
            ts835.BPR_FinancialInformation.TotalPremiumPaymentAmount_02.Should().Be("360");
            ts835.BPR_FinancialInformation.CreditorDebitFlagCode_03.Should().Be("C");
            ts835.BPR_FinancialInformation.PaymentMethodCode_04.Should().Be("ACH");
            ts835.BPR_FinancialInformation.PaymentFormatCode_05.Should().Be("CCP");
            ts835.BPR_FinancialInformation.DepositoryFinancialInstitutionDFIIdentificationNumberQualifier_06.Should().Be("01");

            var result = await clobConverter.Ts835ToClob(ts835);
            result.Should().Be(message);
        }

        [Fact]
        public async Task ClobTo835AndBack__WithEmptyClob_ShouldReturnNull()
        {
            var clobConverter = new ClobConverter();
            var ts835 = await clobConverter.ClobTo835(string.Empty);
            ts835.Should().BeNull();
        }

        [Fact]
        public async Task ClobTo835AndBack__WithNullClob_ShouldReturnNull()
        {
            var clobConverter = new ClobConverter();
            var ts835 = await clobConverter.ClobTo835(null);
            ts835.Should().BeNull();
        }

        [Fact]
        public async Task Ts835ToClobAndBack__WithNullTs835_ThrowsArgumentNullException()
        {
            var clobConverter = new ClobConverter();
            await Assert.ThrowsAsync<ArgumentNullException>(() => clobConverter.Ts835ToClob(null));
        }

        [Fact]
        public async Task Ts835ToClobAndBack__WithEmptyTs835_ReturnsEmptyEnvelope()
        {
            var clobConverter = new ClobConverter();
            var ts835 = new TS835
            {
                ST = new ST { TransactionSetControlNumber_02 = string.Empty }
            };
            var result = await clobConverter.Ts835ToClob(ts835);
            result.Should().Be("ST~SE*2~");
        }

        [Fact]
        public async Task ClobTo835AndBack__WithInvalidClob_ShouldReturnNull()
        {
            var clobConverter = new ClobConverter();
            var invalidClob = "Invalid data format";

            var ts835 = await clobConverter.ClobTo835(invalidClob);
            ts835.Should().BeNull();
        }

        [Fact]
        public async Task ClobTo835AndBack_ProviderAdjustmentWithValidCompositeData_HandlesCorrectly()
        {
            var message = "ST*835*000000116~BPR*I*0*C*NON************20250506~TRN*1*1609872_509594981*1367485085*000091136~REF*EV*460891463~N1*PR*WELFARE PENSION AND ADMINISTRATION~N3*PO BOX 34176~N4*SEATTLE*WA*981241176~REF*2U*91136~REF*EO*1367485085~PER*BL*KEN CASSADY~N1*PE*SOUTHWEST OHIO ENT SPECIALIS*XX*1952895773~N3*PO BOX 714015~N4*CINCINNATI*OH*45271~REF*TJ*310644802~LX*1~CLP*309287*22*-155*-15.88**12*1609872~NM1*QC*1*BROWN*WILLIAM****MI*1972278~REF*1L*59~DTM*050*20250219~SVC*HC:92526:GN*-155*-15.88~DTM*472*20250204~CAS*OA*23*-139.12~AMT*B6*-79.4~SVC*HC:92526:GN*155*0~DTM*472*20250204~CAS*CO*45*155~PLB*310644802*20251231*FB:1609872_509594981*-15.88~SE*28*000000116~";
            var clobConverter = new ClobConverter();
            var ts835 = await clobConverter.ClobTo835(message);

            ts835.Should().NotBeNull();
            ts835.ST.Should().NotBeNull();
            ts835.ST.TransactionSetControlNumber_02.Should().Be("000000116");

            // BPR*I*0*C*NON************20250506~
            ts835.BPR_FinancialInformation.Should().NotBeNull();
            ts835.BPR_FinancialInformation.TransactionHandlingCode_01.Should().Be("I");
            ts835.BPR_FinancialInformation.TotalPremiumPaymentAmount_02.Should().Be("0");
            ts835.BPR_FinancialInformation.CreditorDebitFlagCode_03.Should().Be("C");
            ts835.BPR_FinancialInformation.PaymentMethodCode_04.Should().Be("NON");
            ts835.BPR_FinancialInformation.PaymentFormatCode_05.Should().BeNull();
            ts835.BPR_FinancialInformation.DepositoryFinancialInstitutionDFIIdentificationNumberQualifier_06.Should().BeNull();
            ts835.PLB_ProviderAdjustment[0].Should().NotBeNull();

            // TRN*1*1609872_509594981*1367485085*000091136~
            ts835.TRN_ReassociationTraceNumber.Should().NotBeNull();
            ts835.TRN_ReassociationTraceNumber.TraceTypeCode_01.Should().Be("1");
            ts835.TRN_ReassociationTraceNumber.CurrentTransactionTraceNumber_02.Should().Be("1609872_509594981");
            ts835.TRN_ReassociationTraceNumber.OriginatingCompanyIdentifier_03.Should().Be("1367485085");
            ts835.TRN_ReassociationTraceNumber.ReferenceIdentification_04.Should().Be("000091136");

            // PLB*310644802*20251231*FB:1609872_509594981*-15.88~
            ts835.PLB_ProviderAdjustment[0].AdjustmentIdentifier_03.AdjustmentReasonCode_01.Should().Be("FB");
            ts835.PLB_ProviderAdjustment[0].AdjustmentIdentifier_03.ProviderAdjustmentIdentifier_02.Should().Be("1609872_509594981");
            ts835.PLB_ProviderAdjustment[0].ProviderAdjustmentAmount_04.Should().Be("-15.88");

            var result = await clobConverter.Ts835ToClob(ts835);
            result.Should().Be(message);
        }

        [Fact]
        public async Task ClobTo835AndBack_ProviderAdjustmentWithInvalidCompositeData_TruncatesResult()
        {
            var message = "ST*835*000000116~BPR*I*0*C*NON************20250506~TRN*1*1609872_509594981*1367485085*000091136~REF*EV*460891463~N1*PR*WELFARE PENSION AND ADMINISTRATION~N3*PO BOX 34176~N4*SEATTLE*WA*981241176~REF*2U*91136~REF*EO*1367485085~PER*BL*KEN CASSADY~N1*PE*SOUTHWEST OHIO ENT SPECIALIS*XX*1952895773~N3*PO BOX 714015~N4*CINCINNATI*OH*45271~REF*TJ*310644802~LX*1~CLP*309287*22*-155*-15.88**12*1609872~NM1*QC*1*BROWN*WILLIAM****MI*1972278~REF*1L*59~DTM*050*20250219~SVC*HC:92526:GN*-155*-15.88~DTM*472*20250204~CAS*OA*23*-139.12~AMT*B6*-79.4~SVC*HC:92526:GN*155*0~DTM*472*20250204~CAS*CO*45*155~PLB*310644802*20251231*FB:1609872:509594981*-15.88~SE*28*000000116~";
            var clobConverter = new ClobConverter();
            var ts835 = await clobConverter.ClobTo835(message);

            ts835.Should().NotBeNull();
            ts835.ST.Should().NotBeNull();
            ts835.ST.TransactionSetControlNumber_02.Should().Be("000000116");

            // BPR*I*0*C*NON************20250506~
            ts835.BPR_FinancialInformation.Should().NotBeNull();
            ts835.BPR_FinancialInformation.TransactionHandlingCode_01.Should().Be("I");
            ts835.BPR_FinancialInformation.TotalPremiumPaymentAmount_02.Should().Be("0");
            ts835.BPR_FinancialInformation.CreditorDebitFlagCode_03.Should().Be("C");
            ts835.BPR_FinancialInformation.PaymentMethodCode_04.Should().Be("NON");
            ts835.BPR_FinancialInformation.PaymentFormatCode_05.Should().BeNull();
            ts835.BPR_FinancialInformation.DepositoryFinancialInstitutionDFIIdentificationNumberQualifier_06.Should().BeNull();
            ts835.PLB_ProviderAdjustment[0].Should().NotBeNull();

            // TRN*1*1609872_509594981*1367485085*000091136~
            ts835.TRN_ReassociationTraceNumber.Should().NotBeNull();
            ts835.TRN_ReassociationTraceNumber.TraceTypeCode_01.Should().Be("1");
            ts835.TRN_ReassociationTraceNumber.CurrentTransactionTraceNumber_02.Should().Be("1609872_509594981");
            ts835.TRN_ReassociationTraceNumber.OriginatingCompanyIdentifier_03.Should().Be("1367485085");
            ts835.TRN_ReassociationTraceNumber.ReferenceIdentification_04.Should().Be("000091136");

            // PLB*310644802*20251231*FB:1609872:509594981*-15.88~
            ts835.PLB_ProviderAdjustment[0].AdjustmentIdentifier_03.AdjustmentReasonCode_01.Should().Be("FB");
            ts835.PLB_ProviderAdjustment[0].AdjustmentIdentifier_03.ProviderAdjustmentIdentifier_02.Should().Be("1609872");
            ts835.PLB_ProviderAdjustment[0].ProviderAdjustmentAmount_04.Should().BeNull();

            var result = await clobConverter.Ts835ToClob(ts835);
            result.Should().NotBe(message);
        }
    }
}
