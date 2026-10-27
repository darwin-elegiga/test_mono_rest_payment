using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Pdf.Tools;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class CoverVPaygeGeneratorService : ICoverVPaygeGeneratorService
    {
        private readonly IPythonRunner _pythonRunner;
        private readonly IPdfTools _pdfTools;
        private readonly ILogger<CoverVPaygeGeneratorService> _logger;

        public CoverVPaygeGeneratorService(
            IPythonRunner pythonRunner,
            IPdfTools pdfTools,
            ILogger<CoverVPaygeGeneratorService> logger)
        {
            ThrowIf.IsNull(pythonRunner, nameof(pythonRunner));
            ThrowIf.IsNull(logger, nameof(logger));
            ThrowIf.IsNull(pdfTools, nameof(pdfTools));

            _pythonRunner = pythonRunner;
            _pdfTools = pdfTools;
            _logger = logger;
        }

        public async Task<IEnumerable<VPaygeGeneratedFile>> GenerateCoverVPayges(IEnumerable<VPaygeDocumentType> documents, DocGenParamInternal param)
        {
            var docGenParam = param.DocGenParam;
            var baseFilePath = param.SavePath;

            List<VPaygeGeneratedFile> result = new List<VPaygeGeneratedFile>();

            _logger.LogInformation($"Document Generated Path = {baseFilePath}");

            CoverPageData coverPageData = null;

            foreach (var doc in documents)
            {
                _logger.LogInformation($"VPayge Document Type = {doc}");
                if (coverPageData == null)
                {
                    coverPageData = CreateCoverPageData(param);
                }

                var page = await GenerateVPaygeFile(doc, coverPageData, param);

                if (page != null)
                {
                    if (page.FileName != null && Path.GetExtension(page.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(page.FileName))
                        {
                            page.PageCount = await _pdfTools.GetNumberOfPagesAsync(page.FileName);
                            if (!param.DocGenParam.IsRedCard &&
                                (coverPageData.PAYTYPE == "CLCCK" || coverPageData.PAYTYPE == "CLCHK"))
                            {
                                var maskedFileName = page.FileName.Replace(Storage.Constants.FileExtensions.VPaygeDefaultPdf, ".MASK_cover_cover.pdf");
                                string pdfPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources/Images/tiltedcopy.pdf");
                                string tempFile;
                                File.Copy(page.FileName, tempFile = Path.Combine(Path.GetDirectoryName(page.FileName), Path.GetTempFileName()), true);
                                File.WriteAllBytes(tempFile, await _pdfTools.MicrStampAsync(File.ReadAllBytes(tempFile)));
                                await _pdfTools.StampAsync(maskedFileName, tempFile, pdfPath);
                                File.Delete(tempFile);
                                VPaygeGeneratedFile maskedCheck = new VPaygeGeneratedFile()
                                {
                                    TransactionId = page.TransactionId,
                                    FileName = maskedFileName,
                                    FileDetailType = FileDetailType.Pdf,
                                    Description = "Masked Check",
                                    PageCount = page.PageCount,
                                    VPaygeDocumentType = VPaygeDocumentType.Check
                                };
                                result.Add(maskedCheck);
                            }
                        }
                    }
                    result.Add(page);
                }
            }

            return result;
        }

        private async Task<VPaygeGeneratedFile> GenerateVPaygeFile(VPaygeDocumentType document, CoverPageData coverPageData, DocGenParamInternal paramInternal)
        {
            string fileName = "";
            string savedLocation = "";
            string description = "";
            FileDetailType detailType = FileDetailType.Hidden;

            var cmEnvName = paramInternal.Config.CoverMakerProgramName;
            var tpaConfigFile = paramInternal.Config.TpaConfigFile;

            using (_logger.BeginScope(new Dictionary<string, object>()
                {
                    { "ResultFile", fileName },
                    { "TpaConfigFile", tpaConfigFile },
                }))
            {
                string arg;

                switch (document)
                {
                    case VPaygeDocumentType.VCard_Masked:
                        arg = PayType.CLVCP + "_masked";
                        description = "CLVCP MASKED cover sheet";
                        if (paramInternal.DocGenParam.IsRedCard)
                        {
                            detailType = FileDetailType.Pdf;
                        }
                        break;

                    case VPaygeDocumentType.VCard_Clear:
                        arg = PayType.CLVCP + "_clear";
                        description = "CLVCP CLEAR cover sheet";
                        break;

                    case VPaygeDocumentType.EFT:
                        arg = PayType.CLEFT;
                        description = "CLEFT cover sheet";
                        break;

                    case VPaygeDocumentType.NoPay:
                        arg = PayType.CLNPF;
                        description = "CLNPF cover sheet";
                        break;

                    case VPaygeDocumentType.Client_Check:
                        arg = PayType.CLCCK;
                        description = "CLCCK cover sheet";
                        break;

                    case VPaygeDocumentType.Client_EFT:
                        arg = PayType.CLCFT;
                        description = "CLEFT cover sheet";
                        break;

                    case VPaygeDocumentType.SDA:
                        arg = PayType.CLSDA;
                        description = "CLEFT cover sheet";
                        break;

                    case VPaygeDocumentType.OptumAch:
                        arg = PayType.OPACH;
                        description = "CLEFT cover sheet";
                        break;

                    default:
                        arg = PayType.CLCHK;
                        description = "CLCHK cover sheet";

                        SetCheckInfo(coverPageData, paramInternal.DocGenParam);
                        break;
                }

                var vpaygePythonVersion = paramInternal.Config.CoverMakerPythonVersion;
                _logger.LogInformation("Calling Method '{PythonMethod}' in Python {PythonVersion} script: {PythonScript}", arg, vpaygePythonVersion.Replace(Environment.NewLine, string.Empty), cmEnvName.Replace(Environment.NewLine, string.Empty));

                fileName = paramInternal.GetVPaygeFileName(document, paramInternal.DocGenParam.IsRedCard);

                savedLocation = await _pythonRunner.CreateVPayge(cmEnvName, arg, fileName, coverPageData, tpaConfigFile, vpaygePythonVersion);
            }

            if (string.IsNullOrWhiteSpace(savedLocation))
            {
                _logger.LogError("Could not generate VPayge");
                return null;
            }

            return new VPaygeGeneratedFile
            {
                VPaygeDocumentType = document,
                FileName = fileName,
                Description = description,
                FileDetailType = detailType,
                TransactionId = paramInternal.DocGenParam.StatusRecord.TXID
            };
        }

        private void SetCheckInfo(CoverPageData coverPageData, DocGenParam docGenParam)
        {
            // do nothing
            if (docGenParam.IsRedCard)
            {
                return;
            }
            else if (docGenParam.CheckInfo == null)
            {
                throw new Exception($"You must have CheckInfo for PayType {PayType.CLCHK}");
            }

            coverPageData.one_check_dict = JsonConvert.DeserializeObject<ExpandoObject>(docGenParam.CheckInfo.ToJsonStringSnakeCase());

            coverPageData.one_check_dict["check_amount"] = SetupCheckAmount(coverPageData.one_check_dict["check_amount"]);
        }

        private string SetupCheckAmount(object value)
        {
            var resAsString = value.ToString();

            if (Decimal.TryParse(resAsString, out var parse))
            {
                return string.Format("{0:0.00}", parse);
            }
            return "";
        }

        private CoverPageData CreateCoverPageData(DocGenParamInternal param)
        {
            var coverPageData = CreateCoverPageData(param.DocGenParam, param.Config, param.EobAsDictionary);

            SetupSendBackData(param.DocGenParam, coverPageData);

            return coverPageData;
        }

        private CoverPageData CreateCoverPageData(DocGenParam vPayge, DocSysConfig config, IDictionary<string, dynamic> eob)
        {
            var result = new CoverPageData();

            result.OTB_ENTRY = vPayge.OtbRecord;

            // needed for all the cover pages
            result.BILLING_ENTITY = vPayge.GetValue("BillingEntity", vPayge.StatusRecord.BILENT);

            //following item used in post big_batch_process processing.
            result.DIST = vPayge.StatusRecord.DIST;
            result.PAYTYPE = vPayge.StatusRecord.PAYTYPE;
            result.DESC = vPayge.StatusRecord.DESC;
            result.ERR_CODE = vPayge.StatusRecord.ERR_CODE;
            result.ERR_MSG = vPayge.StatusRecord.ERR_MSG;
            result.FIRST_PMT = vPayge.StatusRecord.FIRST_PMT;

            var vpayPhone = vPayge.OtbRecord.TryGetValueDefault("VPay_Phone");

            result.se_phone_number = vPayge.IsRedCard ? vpayPhone : config.SePhoneNumber;
            result.VPay_Phone = vpayPhone;

            var returnAddress = config.ReturnAddress.Split('|')
                .ToList();

            if (returnAddress.Count < 3)
            {
                returnAddress.AddRange(new string[3 - returnAddress.Count]);
            }

            result.return_line_1 = returnAddress[0];
            result.return_line_2 = returnAddress[1];
            result.return_line_3 = returnAddress[2];

            if (eob.Key("vpay_recipient")?.ContainsKey("line") ?? false)
            {
                var lines = ((List<object>)eob["vpay_recipient"]["line"])
                    .Select(Convert.ToString)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(_ => _.Trim()) // red card trims the lines
                    .ToList();

                if (lines.Count < 5)
                {
                    lines.AddRange(new string[5 - lines.Count]);
                }

                result.mail_to_line_1 = lines[0];
                result.mail_to_line_2 = lines[1];
                result.mail_to_line_3 = lines[2];
                result.mail_to_line_4 = lines[3];
                result.mail_to_line_5 = lines[4];
            }

            if (vPayge.IsRedCard)
            {
                result.contact_name = vPayge.OtbRecord.TryGetValueDefault("OriginName");
                result.contact_number = vPayge.OtbRecord.TryGetValueDefault("TPA_Phone");
                result.tpa_full_name = vPayge.OtbRecord.TryGetValueDefault("TPA_Full_Name");
            }
            else
            {
                if (eob.ContainsKey("vpay_specific"))
                {
                    var vpayDict = eob["vpay_specific"];

                    result.contact_name = Convert.ToString(vpayDict["contact_name"]);
                    result.contact_number = Convert.ToString(vpayDict["contact_number"]);
                    result.tpa_full_name = vpayDict["tpa_fullname"];
                }
            }

            result.tpa_name = vPayge.GetValue("BillingEntity", config.TpaFullName);
            result.TPA_Name = vPayge.OtbRecord.TryGetValueDefault("TPA_Name");

            result.be_name = vPayge.OtbRecord.TryGetValueDefault("BillingEntityName");

            result.setxid = vPayge.StatusRecord.TXID.TrimStart('0'); //strip left 0
            result.claim_id = vPayge.StatusRecord.CLAIMID;
            result.amount = vPayge.StatusRecord.AMOUNT.ToString("F2");

            result.claim_date = vPayge.StatusRecord.CLAIMDATE.ToString("MM/dd/yyyy");
            result.check_date = result.claim_date;
            result.load_date = vPayge.StatusRecord.LOADDATE.ToString("MM/dd/yyyy");

            if (String.Equals(config.CoverDate, "CLAIM_DATE", StringComparison.OrdinalIgnoreCase))
            {
                result.cover_date = result.claim_date;
            }
            else
            {
                result.cover_date = result.load_date;
            }

            var pan = vPayge.StatusRecord.PAN + "";
            result.clear_pan = Regex.Replace(pan, @"(\w{4})(\w{4})(\w{4})(\w{4})", @"$1 $2 $3 $4");
            result.masked_pan = Regex.Replace(pan, @"(\w{4})(\w{2})(\w{6})(\w{4})", @"$1 $2** **** $4");

            result.cvc = vPayge.StatusRecord.CVC;

            if (DateTime.TryParseExact(vPayge.StatusRecord.EXPDATE, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expireDate))
            {
                result.cc_date = expireDate.ToString("MM/yy");
                result.expire_date = expireDate.ToString("MM/dd/yyyy");
            }

            if (eob.Key("claim")?.ContainsKey("paymentid") ?? false)
            {
                result.payment_id = Convert.ToString(eob.Key("claim")["paymentid"]);
            }

            result.check_num_original = GetCheckNumOriginal(eob);
            result.payee_type = GetPayeeType(eob);

            if (eob.Key("eob_data")?.Key("eob")?.ContainsKey("pdf_id") ?? false)
            {
                result.pdf_id = Convert.ToString(eob.Key("eob_data")?.Key("eob")["pdf_id"]);
            }
            else if (eob.Key("eob")?.ContainsKey("pdf_id") ?? false)
            {
                result.pdf_id = Convert.ToString(eob.Key("eob")["pdf_id"]);
            }

            result.eob_xml_dict = eob;
            result.ftp_dict = vPayge.StatusRecord.ToDictionary();

            result.ss_flag = vPayge.StatusRecord.SS_FLAG;
            result.ss_preauth = vPayge.StatusRecord.SS_PREAUTH;
            result.ss_sid = vPayge.StatusRecord.SS_SID;
            result.ss_mid = vPayge.StatusRecord.SS_MID;

            return result;
        }

        private void SetupSendBackData(DocGenParam vPayge, CoverPageData coverPageData)
        {
            if (vPayge.StatusRecord.DIST != DistType.SendBack)
            {
                return;
            }

            _logger.LogInformation("Setup Sendback data");

            if (vPayge.SubstituteDataInfo == null)
            {
                var parameterName = String.Join(".", new string[] { nameof(vPayge), nameof(vPayge.StatusRecord), nameof(vPayge.StatusRecord.DIST) });
                throw new ArgumentException($"{nameof(vPayge.SubstituteDataInfo)} cannot be null when {parameterName} == '{DistType.SendBack}'.", nameof(vPayge));
            }

            coverPageData.mail_to_line_1 = vPayge.SubstituteDataInfo.PayeeName;
            coverPageData.mail_to_line_2 = vPayge.SubstituteDataInfo.Address1;

            if (!String.IsNullOrWhiteSpace(vPayge.SubstituteDataInfo.Address2))
            {
                coverPageData.mail_to_line_3 = vPayge.SubstituteDataInfo.Address2;
                coverPageData.mail_to_line_4 = vPayge.SubstituteDataInfo.CityStateZip;
                coverPageData.mail_to_line_5 = "";
            }
            else
            {
                coverPageData.mail_to_line_3 = vPayge.SubstituteDataInfo.CityStateZip;
                coverPageData.mail_to_line_4 = "";
                coverPageData.mail_to_line_5 = "";
            }

            coverPageData.PAYTYPE = vPayge.SubstituteDataInfo.PayType;
            coverPageData.DIST = vPayge.SubstituteDataInfo.OrigDist;
        }

        private string GetCheckNumOriginal(IDictionary<string, dynamic> eobDictionary)
        {
            var claim = eobDictionary.Key("claim");
            var checkNumOriginal = claim.TryGetValueDefault("checknumoriginal");

            if (checkNumOriginal != null)
            { return Convert.ToString(checkNumOriginal); }

            var paymentId = claim.TryGetValueDefault("paymentId");

            if (paymentId != null)
            {
                var paymentIdString = Convert.ToString(paymentId);
                return paymentIdString.Substring(paymentIdString.Length - 16).Trim('0');
            }

            return null;
        }

        private string GetPayeeType(IDictionary<string, dynamic> eobDictionary)
        {
            var recip = eobDictionary
                .Key("other")
                .TryGetValueDefault("recip");

            return (Convert.ToString(recip) ?? string.Empty).PadRight(1, ' ');
        }
    }
}
