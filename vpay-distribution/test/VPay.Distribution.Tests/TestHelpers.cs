using Bogus;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using VPay.Distribution.Common;
using VPay.Distribution.Common.Helpers;
using VPay.DocSys.Common;
using VPay.DocSys.Storage;

namespace VPay.Distribution.Tests
{
    public static class TestHelpers
    {
        private static List<string> _distTypes;
        private static List<string> _payTypes;
        private static List<string> _prtVendorTypes;

        static TestHelpers()
        {
            Randomizer.Seed = new Random(8675309);
            _distTypes = typeof(DistType).GetAllPublicConstantValues<string>();
            _payTypes = typeof(PayType).GetAllPublicConstantValues<string>();
            _prtVendorTypes = typeof(PrintVendorType).GetAllPublicConstantValues<string>();
        }

        public static DistributionMessage GetDistributionMessageDefault()
        {
            var fileSystem = new MockFileSystem();
            var year = "2018";
            var month = "06";
            var dayValid = "02";
            var faxBase = fileSystem.Path.GetFullPath("/faxbase/");


            return new DistributionMessage()
            {
                StatusFileName = fileSystem.Path.Combine("ORIGIN", "TPA1", year, month, dayValid, "B000012343_FS3243243.sts"),
            };
        }

        public static IEnumerable<StatusRecordGenerated> GetStatusRecords(int count)
        {
            var results = GetStatusRecordFaker().Generate(count);
            var archiveDetailFaker = GetArchiveDetailFaker();
            var rnd = new Faker();
            foreach(var result in results)
            {
                result.ArchiveFile = new ArchiveFile
                {
                    ArchiveFileId = Convert.ToInt64(result.TXID),
                    TransactionId = Convert.ToInt64(result.TXID),
                    DocumentUniqueId = Guid.NewGuid(),
                    FileName = "junk.zip",
                    PathName = "/path",
                    StageFileKey = "F2343233",
                    Tpa = result.TPA,
                    ReferenceCode = result.TRANID,
                };


                
                var cnt = rnd.Random.Number(900) % 5;
                var list = archiveDetailFaker.Generate(cnt + 1);
                var mainPacketLocation = rnd.Random.Number(cnt);

                for (var i = 0; i < list.Count; i++)
                {
                    list[i].ArchiveFileId = result.ArchiveFile.ArchiveFileId;
                    if (i == mainPacketLocation)
                    {
                        list[i].FileName += PayTypeExtensions.ToFileExtension(result.PAYTYPE);
                    }
                }

                result.ArchiveFile.ArchiveDetails = list;
            }

            return results;
        }

        private static Faker<ArchiveDetail> GetArchiveDetailFaker()
        {
            return new Faker<ArchiveDetail>()
                .RuleFor(o => o.ArchiveDetailId, f => f.IndexFaker)
                .RuleFor(o => o.FileName, f => f.System.FileName())
                .RuleFor(o => o.Description, f => f.Lorem.Text())
                .RuleFor(o => o.PageCount, f => f.Random.Int(1, 40))
                .RuleFor(o => o.FileDetailType, f => f.PickRandom<FileDetailType>());
        }

        private static Faker<StatusRecordGenerated> GetStatusRecordFaker()
        {
            return new Faker<StatusRecordGenerated>()
                .RuleFor(o => o.TPA, f => f.Random.String2(3).ToUpper())
                .RuleFor(o => o.AMOUNT, f => Convert.ToDecimal(f.Commerce.Price()))
                .RuleFor(o => o.BILENT, (f, u) => u.TPA + f.Random.Replace("########"))
                .RuleFor(o => o.CHKSTS, f => "")
                .RuleFor(o => o.CLAIMDATE, f => f.Date.Recent(10))
                .RuleFor(o => o.CLAIMID, f => f.Random.Number(10000000).ToString())
                .RuleFor(o => o.CVC, f => f.Finance.CreditCardCvv())
                .RuleFor(o => o.DESC, f => "RELEASE")
                .RuleFor(o => o.DIST, f => f.PickRandom(_distTypes))
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
                .RuleFor(o => o.PAYTYPE, f => f.PickRandom(_payTypes))
                .RuleFor(o => o.PORTALSTAT, f => "")
                .RuleFor(o => o.PRTLOCAL, f => "")
                .RuleFor(o => o.PRTVENDOR, f => f.PickRandom(_prtVendorTypes))
                .RuleFor(o => o.RecordType, f => "50")
                .RuleFor(o => o.REJCODE, f => "")
                .RuleFor(o => o.SS_FLAG, f => "N")
                .RuleFor(o => o.SS_MID, f => "")
                .RuleFor(o => o.SS_PREAUTH, f => "")
                .RuleFor(o => o.SS_SID, f => "")
                .RuleFor(o => o.STATUS, f => "1000")
                .RuleFor(o => o.TRANID, f => f.Random.Number(10000000).ToString())
                .RuleFor(o => o.TXID, f => f.Random.Number(10000000).ToString().PadLeft(20, '0'));
        }
        public static List<T> GetAllPublicConstantValues<T>(this Type type)
        {
            return type
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(T))
                .Select(x => (T)x.GetRawConstantValue())
                .ToList();
        }
    }
}
