using System;
using System.Collections.Generic;
using System.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Tests.TestModels
{
    public class SubstituteTestModels
    {
        public const string ValidTestFile = @"00||20150113|141639||
23|40539131|123 MAIN STREET|SUITE A|KANSAS CITY|MO|75077|CLCHK1|M|VPAY1 DEFAULT|8885551212|9995551212|IC09|BSP|BSP9999999|01|02|03|04|05|06|
23|40539139|234 MAIN STREET|SUITE B|TULSA|OK|74145|CLCHK3|A|VPAY2 DEFAULT|8885551200|9995550012|IC07|BSP|BSP9999999|11|12|13|14|15|16|
23|40539145|345 MAIN STREET|SUITE C|MEMPHIS|TN|38103|CLCHK6|B|VPAY3 DEFAULT|8885551203|9995550312|IC96|BSP|BSP9999999|21|22|23|24|25|26|
23|40539163|456 MAIN STREET|SUITE D|ATLANTA|GA|30303|CLCHK7|C|VPAY4 DEFAULT|8885551206|9995550912|IC153|BSP|BSP9999999|31|32|33|34|35|36|
23|40539177|567 MAIN STREET|SUITE E|DENVER|CO|80207|CLCHK9|D|VPAY5 DEFAULT|8885551207|9995550412|IC1|BSP|BSP9999999|41|42|43|44|45|46|
99|5|";

        public const string InvalidHeaderTestFile = @"01||20150113|141639||
23|40539131|123 MAIN STREET|SUITE A|KANSAS CITY|MO|75077|CLCHK1|M|VPAY1 DEFAULT|8885551212|9995551212|IC09|BSP|BSP9999999|01|02|03|04|05|06|
99|5|";

        public const string InvalidRecordTypeTestFile = @"00||20150113|141639||
56|40539131|123 MAIN STREET|SUITE A|KANSAS CITY|MO|75077|CLCHK1|M|VPAY1 DEFAULT|8885551212|9995551212|IC09|BSP|BSP9999999|01|02|03|04|05|06|
99|5|";

        public const string DuplicateTXIDTestFile = @"00||20150113|141639||
23|40539131|123 MAIN STREET|SUITE A|KANSAS CITY|MO|75077|CLCHK1|M|VPAY1 DEFAULT|8885551212|9995551212|IC09|BSP|BSP9999999|01|02|03|04|05|06|
23|40539131|234 MAIN STREET|SUITE A|KANSAS CITY|MO|75077|CLCHK1|M|VPAY1 DEFAULT|8885551212|9995551212|IC09|BSP|BSP9999999|01|02|03|04|05|06|
99|5|";

        public const string InvalidRecordLengthTestFile = @"00||20150113|141639||
23|40539131|123 MAIN STREET|SUITE A|KANSAS CITY|MO|75077|CLCHK1|M|VPAY1 DEFAULT|8885551212|9995551212|IC09|BSP|BSP9999999|01|02|03|04|05|06||
99|5|";

        public static IDictionary<string, SubstituteDataInfo> ExpectedTestFileData()
        {
            var list =  new List<SubstituteDataInfo>
            {
                new SubstituteDataInfo()
                {
                    RecordId = "23",
                    TXID = "40539131",
                    Address1 = "123 MAIN STREET",
                    Address2 = "SUITE A",
                    City = "KANSAS CITY",
                    State = "MO",
                    Zip = "75077",
                    CityStateZip = "KANSAS CITY, MO 75077",
                    PayType = "CLCHK1",
                    OrigDist = "M",
                    PayeeName = "VPAY1 DEFAULT",
                    PayeePhone = "8885551212",
                    PayeeFax = "9995551212",
                    OriginName = "IC09",
                    TPA = "BSP",
                    BillingEntity = "BSP9999999",
                    VpGrp01 = "01",
                    VpGrp02 = "02",
                    VpGrp03 = "03",
                    VpGrp04 = "04",
                    VpGrp05 = "05",
                    VpGrp06 = "06"
                },
                new SubstituteDataInfo()
                {
                    RecordId = "23",
                    TXID = "40539139",
                    Address1 = "234 MAIN STREET",
                    Address2 = "SUITE B",
                    City = "TULSA",
                    State = "OK",
                    Zip = "74145",
                    CityStateZip = "TULSA, OK 74145",
                    PayType = "CLCHK3",
                    OrigDist = "A",
                    PayeeName = "VPAY2 DEFAULT",
                    PayeePhone = "8885551200",
                    PayeeFax = "9995550012",
                    OriginName = "IC07",
                    TPA = "BSP",
                    BillingEntity = "BSP9999999",
                    VpGrp01 = "11",
                    VpGrp02 = "12",
                    VpGrp03 = "13",
                    VpGrp04 = "14",
                    VpGrp05 = "15",
                    VpGrp06 = "16"
                },
                new SubstituteDataInfo()
                {
                    RecordId = "23",
                    TXID = "40539145",
                    Address1 = "345 MAIN STREET",
                    Address2 = "SUITE C",
                    City = "MEMPHIS",
                    State = "TN",
                    Zip = "38103",
                    CityStateZip = "MEMPHIS, TN 38103",
                    PayType = "CLCHK6",
                    OrigDist = "B",
                    PayeeName = "VPAY3 DEFAULT",
                    PayeePhone = "8885551203",
                    PayeeFax = "9995550312",
                    OriginName = "IC96",
                    TPA = "BSP",
                    BillingEntity = "BSP9999999",
                    VpGrp01 = "21",
                    VpGrp02 = "22",
                    VpGrp03 = "23",
                    VpGrp04 = "24",
                    VpGrp05 = "25",
                    VpGrp06 = "26"
                },
                new SubstituteDataInfo()
                {
                    RecordId = "23",
                    TXID = "40539163",
                    Address1 = "456 MAIN STREET",
                    Address2 = "SUITE D",
                    City = "ATLANTA",
                    State = "GA",
                    Zip = "30303",
                    CityStateZip = "ATLANTA, GA 30303",
                    PayType = "CLCHK7",
                    OrigDist = "C",
                    PayeeName = "VPAY4 DEFAULT",
                    PayeePhone = "8885551206",
                    PayeeFax = "9995550912",
                    OriginName = "IC153",
                    TPA = "BSP",
                    BillingEntity = "BSP9999999",
                    VpGrp01 = "31",
                    VpGrp02 = "32",
                    VpGrp03 = "33",
                    VpGrp04 = "34",
                    VpGrp05 = "35",
                    VpGrp06 = "36"
                },
                new SubstituteDataInfo()
                {
                    RecordId = "23",
                    TXID = "40539177",
                    Address1 = "567 MAIN STREET",
                    Address2 = "SUITE E",
                    City = "DENVER",
                    State = "CO",
                    Zip = "80207",
                    CityStateZip = "DENVER, CO 80207",
                    PayType = "CLCHK9",
                    OrigDist = "D",
                    PayeeName = "VPAY5 DEFAULT",
                    PayeePhone = "8885551207",
                    PayeeFax = "9995550412",
                    OriginName = "IC1",
                    TPA = "BSP",
                    BillingEntity = "BSP9999999",
                    VpGrp01 = "41",
                    VpGrp02 = "42",
                    VpGrp03 = "43",
                    VpGrp04 = "44",
                    VpGrp05 = "45",
                    VpGrp06 = "46"
                }
            };

            return list.ToDictionary(x => x.TXID, x => x);
        }

    }
}
