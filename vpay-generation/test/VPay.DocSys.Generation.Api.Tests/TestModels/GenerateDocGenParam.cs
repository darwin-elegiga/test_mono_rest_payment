using Bogus;
using System;
using System.Collections.Generic;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation.Api.Tests.TestModels
{
    public static class GenerateDocGenParam
    {

        static GenerateDocGenParam()
        {
            Randomizer.Seed = new Random(8675309);
        }

        public static DocGenParam GetEOBPdfOnly()
        {
            var faker = new Faker();
            var random = new Bogus.Randomizer();
            var ranCompany = new Bogus.DataSets.Company();
            var ranAddress = new Bogus.DataSets.Address();
            var ranPh = new Bogus.DataSets.PhoneNumbers();

            var tpa = random.String2(3).ToUpper();
            var tpaName = ranCompany.CompanyName();
            var vpaPhone = ranPh.PhoneNumber();
            var tpaPhone = ranPh.PhoneNumber();

            var input = "FS" + random.Replace("########");
            var cDate = faker.Date.Recent(10).ToString("yyyyMMdd");

            var statusRecordFaker = GetStatusRecordFaker(tpa);

            var statusRecord = statusRecordFaker.Generate();

            var otb = new OtbRecord()
                {
                    { "PayType", "*" },
                    { "Origin", "IC" },
                    { "OriginName", "InsurCard" },
                    { "TPA", statusRecord.TPA },
                    { "TPA_Name", tpaName.Replace(" ", "") },
                    { "TPA_Full_Name", tpaName },
                    { "VPay_Phone", vpaPhone },
                    { "TPA_Phone", tpaPhone },
                    { "BillingEntity", $"{tpa}{random.Number(9999999).ToString().PadLeft(7, '0')}" },
                    { "BillingEntityName", ranCompany.CompanyName() },
                    { "VpaygeLogo", "insurcard-vpay-logo.jpg" },
                    { "CardLogo", "VP_insurcard-card2-bw-logo.jpg" },
                    { "ReturnAddress", ranAddress.FullAddress() }
                };

            var configuration = new DocSysConfiguration()
            {
                EnvironmentSettings = new Dictionary<string, string>()
                {
                    { "rpt_progname", "/test/eob_maker.py"}
                },
                AppSettings = new Dictionary<string, string>()
            };


            var result = new DocGenParam()
            {
                Configuration = configuration,
                OtbRecord = otb,
                StatusRecord = statusRecord,
                CheckInfo = null,
                InputFile = input,
                CDate = cDate,
                GenerateEOB = true,
                SubstituteDataInfo = null,
                EOBData = new EOB
                {
                    Name = $"{input}.{cDate}.{statusRecord.GUID}.eob.xml",
                    XML = EOB_XML_PDFONLY,
                    PDF = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("This is a pdf"))
                }
            };


            return result;
        }

        private static Faker<StatusRecord> GetStatusRecordFaker(string tpa)
        {
            return new Faker<StatusRecord>()
                .RuleFor(o => o.AMOUNT, f => Convert.ToDecimal(f.Commerce.Price()))
                .RuleFor(o => o.BILENT, f => tpa + f.Random.Replace("########"))
                .RuleFor(o => o.CHKSTS, f => "")
                .RuleFor(o => o.CLAIMDATE, f => f.Date.Recent(10))
                .RuleFor(o => o.CLAIMID, f => f.Random.Number(10000000).ToString())
                .RuleFor(o => o.CVC, f => f.Finance.CreditCardCvv())
                .RuleFor(o => o.DESC, f => "RELEASE")
                .RuleFor(o => o.DIST, f => "Z")
                .RuleFor(o => o.ERR_CODE, f => "0000")
                .RuleFor(o => o.ERR_MSG, f => "")
                .RuleFor(o => o.EXPDATE, f => "10010101")
                .RuleFor(o => o.FIRST_PMT, f => "")
                .RuleFor(o => o.GUID, f => Guid.NewGuid().ToString().ToLower())
                .RuleFor(o => o.LOADDATE, f => f.Date.Recent(10))
                .RuleFor(o => o.NFPYDT_ID, f => f.Random.Number(10000000).ToString())
                .RuleFor(o => o.ORIGIN, f => "IC")
                .RuleFor(o => o.PAN, f => f.Finance.CreditCardNumber())
                .RuleFor(o => o.PAYEENAME, f => f.Person.FullName)
                .RuleFor(o => o.PAYTYPE, f => "CLNPF")
                .RuleFor(o => o.PORTALSTAT, f => "")
                .RuleFor(o => o.PRTLOCAL, f => "")
                .RuleFor(o => o.PRTVENDOR, f => "FSSI")
                .RuleFor(o => o.RecordType, f => "50")
                .RuleFor(o => o.REJCODE, f => "")
                .RuleFor(o => o.SS_FLAG, f => "N")
                .RuleFor(o => o.SS_MID, f => "")
                .RuleFor(o => o.SS_PREAUTH, f => "")
                .RuleFor(o => o.SS_SID, f => "")
                .RuleFor(o => o.STATUS, f => "1000")
                .RuleFor(o => o.TPA, f => tpa)
                .RuleFor(o => o.TRANID, f => f.Random.Number(10000000).ToString())
                .RuleFor(o => o.TXID, f => f.Random.Number(10000000).ToString().PadLeft(20, '0'));
        }


        private static string EOB_XML_PDFONLY = @"<?xml version=""1.0"" encoding=""UTF-8""?><eob_xml>
<eob_method>PDF</eob_method>
</eob_xml>
";
    }
}
