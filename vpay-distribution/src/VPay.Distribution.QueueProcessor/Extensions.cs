using System.Linq;
using VPay.DocSys.Common;

namespace VPay.Distribution.QueueProcessor
{
    public static class Extensions
    {
        public static StatusFile Copy(this StatusFile source)
        {
            if(source == null)
                return null;

            return new StatusFile()
            {
                Header = source.Header.Copy(),
                Records = source.Records.Select(r => r.Copy()).ToList(),
                Footer = source.Footer.Copy()
            };
        }

        public static StatusHeader Copy(this StatusHeader source)
        {
            if(source == null)
                return null;

            return new StatusHeader()
            {
                Count = source.Count,
                CDateTime = source.CDateTime,
                AuthCode = source.AuthCode,
                CustomerFName = source.CustomerFName,
                Destination = source.Destination,
                FileData = source.FileData,
                FileName = source.FileName,
                InputFile = source.InputFile,
                PassCode = source.PassCode,
                ProdDev = source.ProdDev,
                RecordDate = source.RecordDate,
                RecordType = source.RecordType,
                Source = source.Source,
                Total = source.Total
            };
        }

        public static StatusRecord Copy(this StatusRecord source)
        {
            if(source == null)
                return null;

            return new StatusRecord()
            {
                AMOUNT = source.AMOUNT,
                BILENT = source.BILENT,
                CHKSTS = source.CHKSTS,
                CLAIMDATE = source.CLAIMDATE,
                CLAIMID = source.CLAIMID,
                CVC = source.CVC,
                DESC = source.DESC,
                DIST = source.DIST,
                ERR_CODE = source.ERR_CODE,
                ERR_MSG = source.ERR_MSG,
                EXPDATE = source.EXPDATE,
                FIRST_PMT = source.FIRST_PMT,
                GUID = source.GUID,
                LOADDATE = source.LOADDATE,
                NFPYDT_ID = source.NFPYDT_ID,
                ORIGIN = source.ORIGIN,
                PAN = source.PAN,
                PAYEENAME = source.PAYEENAME,
                PAYTYPE = source.PAYTYPE,
                PORTALSTAT = source.PORTALSTAT,
                PRTLOCAL = source.PRTLOCAL,
                PRTVENDOR = source.PRTVENDOR,
                RecordType = source.RecordType,
                REJCODE = source.REJCODE,
                SS_FLAG = source.SS_FLAG,
                SS_MID = source.SS_MID,
                SS_PREAUTH = source.SS_PREAUTH,
                SS_SID = source.SS_SID,
                STATUS = source.STATUS,
                TPA = source.TPA,
                TRANID = source.TRANID,
                TXID = source.TXID,
                USERKEY = source.USERKEY,
            };
        }

        public static StatusFooter Copy(this StatusFooter source)
        {
            if(source == null)
                return null;

            return new StatusFooter()
            {
                RecordType = source.RecordType,
                RecordCount = source.RecordCount,
                Amount = source.Amount
            };
        }
    }
}
