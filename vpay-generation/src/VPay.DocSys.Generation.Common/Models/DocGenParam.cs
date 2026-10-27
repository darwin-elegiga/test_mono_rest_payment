using System;
using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{

    /// <summary>
    /// This is the parameter need to run any of the DocSys generation for a transaction
    /// </summary>
    public class DocGenParam
    {
        /// <summary>
        /// Configuration object needed to generate a document for a transaction
        /// </summary>
        public DocSysConfiguration Configuration { get; set; }

        /// <summary>
        /// The otb record needed to generate a document for a transaction
        /// </summary>
        public OtbRecord OtbRecord { get; set; }

        /// <summary>
        /// The status record needed to generate a document for a transaction
        /// </summary>
        public StatusRecord StatusRecord { get; set; }

        /// <summary>
        /// The eob data needed to generate a document for a transaction (includes the name, xml, and pdf of the xml document)
        /// </summary>
        public EOB EOBData { get; set; }

        /// <summary>
        /// This is the check info needed to generate document if the document type is a check
        /// </summary>
        public CheckInfo CheckInfo { get; set; }

        /// <summary>
        /// This is the name of the input file in the status file's header
        /// </summary>
        public string InputFile { get; set; }

        /// <summary>
        /// This is the date includeed in the status file's header
        /// </summary>
        public string CDate { get; set; }

        /// <summary>
        /// This will determine if we need to generate the EOB document. Currently for Re-issues and Drop to Fax this should be false
        /// </summary>
        public bool GenerateEOB { get; set; }

        /// <summary>
        /// Fully qualified path to the status file (.sts) used to initiate the request.
        /// </summary>
        public string StatusFilePath { get; set; }

        /// <summary>
        /// Flag for Red Card process
        /// </summary>
        public bool IsRedCard { get; set; }

        /// <summary>
        /// Flag for Custom SendBack process
        /// </summary>
        public bool IsCustomSendBack { get; set; }

        /// <summary>
        /// This substitute data used in vpayge generation if a "sub" file exists
        /// </summary>
        public SubstituteDataInfo SubstituteDataInfo { get; set; }
        public bool RegeneratePackage { get; set; }

        /// <summary>
        /// Removes newlines from any and all string datatypes nested within each property in this class
        /// </summary>
        /// <returns>A new sanitized DocGenParam that can be used for logging</returns>
        public DocGenParam Sanitize()
        {
            // Populate this object with sanatized properties
            var returnParam = new DocGenParam
            {
                InputFile = InputFile?.Replace(Environment.NewLine, ""),
                CDate = CDate?.Replace(Environment.NewLine, ""),
                GenerateEOB = GenerateEOB,
                StatusFilePath = StatusFilePath?.Replace(Environment.NewLine, ""),
                IsRedCard = IsRedCard,
                IsCustomSendBack = IsCustomSendBack,
                RegeneratePackage = RegeneratePackage
            };

            // Scrub all the values in Configuration.AppSettings and Configuration.EnvironmentSettings dictionaries
            var docSysConfiguration = new DocSysConfiguration();
            docSysConfiguration.AppSettings = new Dictionary<string, string>();
            docSysConfiguration.EnvironmentSettings = new Dictionary<string, string>();
            if (Configuration is not null)
            {
                if (Configuration.AppSettings is not null)
                {
                    foreach (var key in Configuration.AppSettings.Keys)
                    {
                        docSysConfiguration.AppSettings.Add(
                            key?.Replace(Environment.NewLine, ""),
                            Configuration.AppSettings[key]?.Replace(Environment.NewLine, ""));
                    }
                }

                if (Configuration.EnvironmentSettings is not null)
                {
                    foreach (var key in Configuration.EnvironmentSettings.Keys)
                    {
                        docSysConfiguration.EnvironmentSettings.Add(
                            key?.Replace(Environment.NewLine, ""),
                            Configuration.EnvironmentSettings[key]?.Replace(Environment.NewLine, ""));
                    }
                }                
            }
            
            returnParam.Configuration = docSysConfiguration;

            // Scrub all the values in OtbRecord dictionary
            var otbRecord = new OtbRecord();
            if (OtbRecord is not null)
            {
                foreach (var key in OtbRecord.Keys)
                {
                    otbRecord.Add(
                        key?.Replace(Environment.NewLine, ""),
                        OtbRecord[key]?.Replace(Environment.NewLine, ""));
                }
            }
            
            returnParam.OtbRecord = otbRecord;

            // Populate StatusRecord with sanatized values
            returnParam.StatusRecord = StatusRecord is null
                ? new StatusRecord()
                : new StatusRecord
            {
                AMOUNT = StatusRecord.AMOUNT,
                BILENT = StatusRecord.BILENT?.Replace(Environment.NewLine, ""),
                CHKSTS = StatusRecord.CHKSTS?.Replace(Environment.NewLine, ""),
                CLAIMDATE = StatusRecord.CLAIMDATE,
                CLAIMID = StatusRecord.CLAIMID?.Replace(Environment.NewLine, ""),
                CVC = StatusRecord.CVC?.Replace(Environment.NewLine, ""),
                DESC = StatusRecord.DESC?.Replace(Environment.NewLine, ""),
                DIST = StatusRecord.DIST?.Replace(Environment.NewLine, ""),
                ERR_CODE = StatusRecord.ERR_CODE?.Replace(Environment.NewLine, ""),
                ERR_MSG = StatusRecord.ERR_MSG?.Replace(Environment.NewLine, ""),
                EXPDATE = StatusRecord.EXPDATE?.Replace(Environment.NewLine, ""),
                FIRST_PMT = StatusRecord.FIRST_PMT?.Replace(Environment.NewLine, ""),
                GUID = StatusRecord.GUID?.Replace(Environment.NewLine, ""),
                LOADDATE = StatusRecord.LOADDATE,
                NFPYDT_ID = StatusRecord.NFPYDT_ID?.Replace(Environment.NewLine, ""),
                ORIGIN = StatusRecord.ORIGIN?.Replace(Environment.NewLine, ""),
                PAN = StatusRecord.PAN?.Replace(Environment.NewLine, ""),
                PAYEENAME = StatusRecord.PAYEENAME?.Replace(Environment.NewLine, ""),
                PAYTYPE = StatusRecord.PAYTYPE?.Replace(Environment.NewLine, ""),
                PORTALSTAT = StatusRecord.PORTALSTAT?.Replace(Environment.NewLine, ""),
                PRTLOCAL = StatusRecord.PRTLOCAL?.Replace(Environment.NewLine, ""),
                PRTVENDOR = StatusRecord.PRTVENDOR?.Replace(Environment.NewLine, ""),
                RecordType = StatusRecord.RecordType?.Replace(Environment.NewLine, ""),
                REJCODE = StatusRecord.REJCODE?.Replace(Environment.NewLine, ""),
                SS_FLAG = StatusRecord.SS_FLAG?.Replace(Environment.NewLine, ""),
                SS_MID = StatusRecord.SS_MID?.Replace(Environment.NewLine, ""),
                SS_PREAUTH = StatusRecord.SS_PREAUTH?.Replace(Environment.NewLine, ""),
                SS_SID = StatusRecord.SS_SID?.Replace(Environment.NewLine, ""),
                STATUS = StatusRecord.STATUS?.Replace(Environment.NewLine, ""),
                TPA = StatusRecord.TPA?.Replace(Environment.NewLine, ""),
                TRANID = StatusRecord.TRANID?.Replace(Environment.NewLine, ""),
                TXID = StatusRecord.TXID?.Replace(Environment.NewLine, ""),
                USERKEY = StatusRecord.USERKEY?.Replace(Environment.NewLine, ""),
                CORRESP_ID = StatusRecord.CORRESP_ID,
                FAX_NUMBER = StatusRecord.FAX_NUMBER?.Replace(Environment.NewLine, ""),
                TIN = StatusRecord.TIN?.Replace(Environment.NewLine, "")
            };

            // Populate EOBData with sanatized values
            returnParam.EOBData = EOBData is null
                ? new EOB()
                :new EOB
            {
                Name = EOBData?.Name?.Replace(Environment.NewLine, ""),
                XML = EOBData?.XML?.Replace(Environment.NewLine, ""),
                PDF = EOBData?.PDF?.Replace(Environment.NewLine, "")
            };

            // Populate CheckInfo with sanatized values
            returnParam.CheckInfo = CheckInfo is null
                ? new CheckInfo()
                : new CheckInfo
            {
                BankRoutingNumber = CheckInfo.BankRoutingNumber?.Replace(Environment.NewLine, ""),
                BankName = CheckInfo.BankName?.Replace(Environment.NewLine, ""),
                ClientPaymentNumber = CheckInfo.ClientPaymentNumber?.Replace(Environment.NewLine, ""),
                TPA = CheckInfo.TPA?.Replace(Environment.NewLine, ""),
                BankAddress = CheckInfo.BankAddress?.Replace(Environment.NewLine, ""),
                SourceName = CheckInfo.SourceName?.Replace(Environment.NewLine, ""),
                SourceAddress = CheckInfo.SourceAddress?.Replace(Environment.NewLine, ""),
                SourceCity = CheckInfo.SourceCity?.Replace(Environment.NewLine, ""),
                SourceState = CheckInfo.SourceState?.Replace(Environment.NewLine, ""),
                SourceZipcode = CheckInfo.SourceZipcode?.Replace(Environment.NewLine, ""),
                CheckAmount = CheckInfo.CheckAmount,
                Payto = CheckInfo.Payto?.Replace(Environment.NewLine, ""),
                MailtoAddressline1 = CheckInfo.MailtoAddressline1?.Replace(Environment.NewLine, ""),
                MailtoAddressline2 = CheckInfo.MailtoAddressline2?.Replace(Environment.NewLine, ""),
                MailtoAddressline3 = CheckInfo.MailtoAddressline3?.Replace(Environment.NewLine, ""),
                MailtoAddressline4 = CheckInfo.MailtoAddressline4?.Replace(Environment.NewLine, ""),
                CheckDate = CheckInfo.CheckDate?.Replace(Environment.NewLine, ""),
                BankAccountNumber = CheckInfo.BankAccountNumber?.Replace(Environment.NewLine, ""),
                VoidAfter = CheckInfo.VoidAfter,
                SETransactionId = CheckInfo.SETransactionId?.Replace(Environment.NewLine, ""),
                SendType = CheckInfo.SendType?.Replace(Environment.NewLine, ""),
                CheckStatus = CheckInfo.CheckStatus?.Replace(Environment.NewLine, ""),
                BE = CheckInfo.BE?.Replace(Environment.NewLine, ""),
                DocGuid = CheckInfo.DocGuid?.Replace(Environment.NewLine, "")
            };            

            // Populate SubstituteDataInfo with sanatized values
            returnParam.SubstituteDataInfo = SubstituteDataInfo is null
                ? new SubstituteDataInfo()
                : new SubstituteDataInfo
            {
                RecordId = SubstituteDataInfo.RecordId?.Replace(Environment.NewLine, ""),
                TXID = SubstituteDataInfo.TXID?.Replace(Environment.NewLine, ""),
                Address1 = SubstituteDataInfo.Address1?.Replace(Environment.NewLine, ""),
                Address2 = SubstituteDataInfo.Address2?.Replace(Environment.NewLine, ""),
                City = SubstituteDataInfo.City?.Replace(Environment.NewLine, ""),
                State = SubstituteDataInfo.State?.Replace(Environment.NewLine, ""),
                Zip = SubstituteDataInfo.Zip?.Replace(Environment.NewLine, ""),
                CityStateZip = SubstituteDataInfo.CityStateZip?.Replace(Environment.NewLine, ""),
                PayType = SubstituteDataInfo.PayType?.Replace(Environment.NewLine, ""),
                OrigDist = SubstituteDataInfo.OrigDist?.Replace(Environment.NewLine, ""),
                PayeeName = SubstituteDataInfo.PayeeName?.Replace(Environment.NewLine, ""),
                PayeePhone = SubstituteDataInfo.PayeePhone?.Replace(Environment.NewLine, ""),
                PayeeFax = SubstituteDataInfo.PayeeFax?.Replace(Environment.NewLine, ""),
                OriginName = SubstituteDataInfo.OriginName?.Replace(Environment.NewLine, ""),
                TPA = SubstituteDataInfo.TPA?.Replace(Environment.NewLine, ""),
                BillingEntity = SubstituteDataInfo.BillingEntity?.Replace(Environment.NewLine, ""),
                VpGrp01 = SubstituteDataInfo.VpGrp01?.Replace(Environment.NewLine, ""),
                VpGrp02 = SubstituteDataInfo.VpGrp02?.Replace(Environment.NewLine, ""),
                VpGrp03 = SubstituteDataInfo.VpGrp03?.Replace(Environment.NewLine, ""),
                VpGrp04 = SubstituteDataInfo.VpGrp04?.Replace(Environment.NewLine, ""),
                VpGrp05 = SubstituteDataInfo.VpGrp05?.Replace(Environment.NewLine, ""),
                VpGrp06 = SubstituteDataInfo.VpGrp06?.Replace(Environment.NewLine, "")
            };

            return returnParam;
        }
    }
}
