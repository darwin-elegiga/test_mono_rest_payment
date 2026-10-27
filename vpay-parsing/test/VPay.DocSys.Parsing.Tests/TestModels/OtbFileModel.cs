using Bogus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VPay.DocSys.Parsing.Tests.TestModels
{
    public class OtbFileModel
    {
        public string PayType { get; set; }
        public string Origin { get; set; }
        public string OriginName { get; set; }
        public string TPA { get; set; }
        public string TPA_Name { get; set; }
        public string TPA_Full_Name { get; set; }
        public string VPay_Phone { get; set; }
        public string TPA_Phone { get; set; }
        public string BillingEntity { get; set; }
        public string BillingEntityName { get; set; }
        public string RawBEName { get; set; }
        public string VpaygeLogo { get; set; }
        public string CardLogo { get; set; }
        public string ReturnAddress { get; set; }


        public static IEnumerable<OtbFileModel> GenerateOtbObjects(int count)
        {
            List<OtbFileModel> result = new List<OtbFileModel>();

            Randomizer.Seed = new Random(8675309);

            var random = new Bogus.Randomizer();
            var ranCompany = new Bogus.DataSets.Company();
            var ranPh = new Bogus.DataSets.PhoneNumbers();

            var origin = random.String2(3).ToUpper();
            var originName = ranCompany.CompanyName();
            var tpa = random.String2(3).ToUpper();
            var vpaPhone = ranPh.PhoneNumber();
            var tpaPhone = ranPh.PhoneNumber();



            var fakerOtb = new Faker<OtbFileModel>()
                .RuleFor(u => u.PayType, f => "*")
                .RuleFor(u => u.Origin, f => origin)
                .RuleFor(u => u.OriginName, f => originName)
                .RuleFor(u => u.TPA, f => tpa)
                .RuleFor(u => u.TPA_Name, f => f.Company.CompanyName())
                .RuleFor(u => u.TPA_Full_Name, (f, u) => u.TPA_Name)
                .RuleFor(u => u.VPay_Phone, f => vpaPhone)
                .RuleFor(u => u.TPA_Phone, f => tpaPhone)
                .RuleFor(u => u.BillingEntity, f => f.Finance.Account())
                .RuleFor(u => u.BillingEntityName, f => f.Company.CompanyName())
                .RuleFor(u => u.RawBEName, f => f.Random.String2(3).ToUpper())
                .RuleFor(u => u.VpaygeLogo, f => f.System.FileName("jpg"))
                .RuleFor(u => u.CardLogo, f => f.System.FileName("jpg"))
                .RuleFor(u => u.ReturnAddress, f => f.Address.FullAddress())
                ;


            return fakerOtb.GenerateForever().Take(count).ToList();
        }
    }
}
