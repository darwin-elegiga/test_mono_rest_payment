using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;
using VPay.Data.Db2.Abstractions.Trn835Data;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Enums;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Common.Models;
using VPay.DocSys.Generation.Data;
using VPay.DocSys.Generation.Extensions;
using VPay.DocSys.Generation.Models;
using System.Text.Json;

namespace VPay.DocSys.Generation;

public class StpRemittanceDocumentService : IStpRemittanceDocumentService
{
    private readonly IStpRemittanceRepository _stpRemittanceRepository;
    private readonly ITrn835DataRepository _trn835DataRepository;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<StpRemittanceDocumentService> _logger;
    private readonly IRemittancePartnerNetworkApiClient _remittancePartnerNetworkApiClient;

    public StpRemittanceDocumentService(IStpRemittanceRepository stpRemittanceRepository, ITrn835DataRepository trn835DataRepository,
        IFileSystem fileSystem, ILogger<StpRemittanceDocumentService> logger,
        IRemittancePartnerNetworkApiClient remittancePartnerNetworkApiClient)
    {
        ArgumentHelpers.ThrowIf.IsNull(stpRemittanceRepository, nameof(stpRemittanceRepository));
        ArgumentHelpers.ThrowIf.IsNull(trn835DataRepository, nameof(trn835DataRepository));
        ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
        ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));
        ArgumentHelpers.ThrowIf.IsNull(remittancePartnerNetworkApiClient, nameof(remittancePartnerNetworkApiClient));

        _stpRemittanceRepository = stpRemittanceRepository;
        _trn835DataRepository = trn835DataRepository;
        _fileSystem = fileSystem;
        _logger = logger;
        _remittancePartnerNetworkApiClient = remittancePartnerNetworkApiClient;
    }

    public async Task<(IEnumerable<StpRemittanceGeneratedFile> files, bool remittanceSent)> GenerateAndSendRemittanceDocument(DocGenParamInternal param, long transactionId, XPathDocument eobXml)
    {
        var fileList = new List<StpRemittanceGeneratedFile>();

        var rawEnrollmentType = param.DocGenParam.StatusRecord.NETWORKENROLLMENTTYPE.Trim();

        if (!Enum.TryParse(rawEnrollmentType, out RemittanceNetworkEnrollmentTypes networkEnrollmentType))
        {
            _logger.LogWarning("Failed to build STP Remittance document. Unknown network enrollment type: {EnrollmentType}", rawEnrollmentType);
            var buildFailedFilename = $"{transactionId}.stp.{rawEnrollmentType}.remittance.invalid.enrollmenttype";
            var buildFailedFilePath = Path.Combine(param.SavePath, buildFailedFilename);
            _fileSystem.File.WriteAllText(buildFailedFilePath, $"Unknown network enrollment type: {rawEnrollmentType}");
            _logger.LogInformation("Created STP Remittance warning: {FileName}", buildFailedFilePath);

            fileList.Add(new StpRemittanceGeneratedFile
            {
                TransactionId = transactionId.ToString(),
                Description = "STP Remittance Document build failure",
                FileDetailType = FileDetailType.Text,
                FileName = buildFailedFilePath,
                PageCount = 1,
            });

            return (fileList, false);
        }

        var filenameBase = $"{transactionId}.stp.{networkEnrollmentType.ToString().ToLower()}.remittance";

        var remitFilename = $"{filenameBase}.json";
        var remitFilePath = Path.Combine(param.SavePath, remitFilename);

        _logger.LogInformation("Creating STP Remittance document: {FileName}", remitFilePath);

        StpRemittanceDocumentBase stpDoc = null;

        try
        {
            var tpaAddress = await _stpRemittanceRepository.GetTpaAddressAsync(param.DocGenParam.StatusRecord.TPA);

            var nfpydtId = Convert.ToInt64(param?.DocGenParam.StatusRecord.NFPYDT_ID);
            var trnData = await _trn835DataRepository.GetTrn835DataByNonFundedPaymentDetailId(nfpydtId, default);

            var networkName = param.DocGenParam.StatusRecord.NETWORKNAME.Trim();
            var networkUuid = param.DocGenParam.StatusRecord.SS_SID?.Trim();
            // Only provide a value if not null or empty; otherwise, set to null so it can be excluded from the document
            networkUuid = networkUuid == null ? null : networkUuid == string.Empty ? null : networkUuid.Trim();

            if (networkEnrollmentType == RemittanceNetworkEnrollmentTypes.C2B)
            {
                stpDoc = new StpC2BRemittanceDocument();
            }
            else if (networkEnrollmentType == RemittanceNetworkEnrollmentTypes.B2B)
            {
                stpDoc = new StpB2BRemittanceDocument();
            }
            
            string xsltResourceName = "VPay.DocSys.Generation.StpC2B_Transform.xslt";
            var eobObjectJson = XmlToJsonConverter.TransformXmlToJson(eobXml, param?.DocGenParam.StatusRecord, xsltResourceName);
            var eobObject = JsonSerializer.Deserialize<EobXsl>(eobObjectJson);
            stpDoc.Create(eobObject, networkName, transactionId, trnData.Segment, tpaAddress, networkUuid);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build STP Remittance document");
            var buildFailedFilename = $"{filenameBase}.build.failed";
            var buildFailedFilePath = Path.Combine(param.SavePath, buildFailedFilename);
            _fileSystem.File.WriteAllText(buildFailedFilePath, ex.ToString());
            _logger.LogInformation("Created STP Remittance error: {FileName}", buildFailedFilePath);

            fileList.Add(new StpRemittanceGeneratedFile
            {
                TransactionId = transactionId.ToString(),
                Description = "STP Remittance Document build failure",
                FileDetailType = FileDetailType.Text,
                FileName = buildFailedFilePath,
                PageCount = 1,
            });

            return (fileList, false);
        }

        var json = stpDoc.ToJsonString();
        var tin = stpDoc.Payee.Tin;

        _fileSystem.File.WriteAllText(remitFilePath, json);
        _logger.LogInformation("Created STP Remittance document: {FileName}", remitFilePath);
        _logger.LogDebug("Sending STP Remittance request (TIN {TaxId}) {EnrollmentType}:\n{RequestBody}", tin, networkEnrollmentType, json);

        fileList.Add(new StpRemittanceGeneratedFile
        {
            TransactionId = transactionId.ToString(),
            Description = "STP Remittance Json",
            FileDetailType = FileDetailType.Hidden,
            FileName = remitFilePath,
            PageCount = 1,
        });

        bool docAccepted = false;
        string responseBody = string.Empty;
        if (networkEnrollmentType == RemittanceNetworkEnrollmentTypes.C2B)
        {
            // Post Remit document immediately, no need to wait for EDI 835 to be available.
            (docAccepted, responseBody) = await _remittancePartnerNetworkApiClient
                .PostRemittanceDocument(networkEnrollmentType, json)
                .ConfigureAwait(false);
        }
        else if (networkEnrollmentType == RemittanceNetworkEnrollmentTypes.B2B)
        {
            // Validate Remit only, Post Remit will be done after EDI 835 is confirmed to be available.
            (docAccepted, responseBody) = await _remittancePartnerNetworkApiClient
                .ValidateRemittanceDocument(networkEnrollmentType, json)
                .ConfigureAwait(false);

            //// If the document was accepted, mark the remit as valid in the database
            //// NOTE: This is now done in Doc Sys Distribution.
            //if (docAccepted)
            //{
            //    // Mark the remit document as validated so it can be posted later.
            //    var valid = await _stpRemittanceRepository.MarkStpPaymentRemitValid(transactionId);
            //    var message = valid == null ? "FAILED to mark" : "MARKED";
            //    _logger.LogDebug("{Message} STP Payment Remit as valid. Transaction ID: {TransactionId}", message, transactionId);
            //}
        }

        var responseFile = $"{filenameBase}.http.response.{(docAccepted ? "success" : "failed")}";
        var responseFilePath = Path.Combine(param.SavePath, responseFile);
        _fileSystem.File.WriteAllText(responseFilePath, responseBody);
        _logger.LogInformation("Created STP Remittance response: {FileName}", responseFilePath);
        _logger.LogDebug("Received STP Remittance response (TIN {TaxId}) {EnrollmentType}:\n{ResponseBody}", tin, networkEnrollmentType, responseBody);

        fileList.Add(new StpRemittanceGeneratedFile
        {
            TransactionId = transactionId.ToString(),
            Description = "STP Remittance Http Response",
            FileDetailType = FileDetailType.Hidden,
            FileName = responseFilePath,
            PageCount = 1,
        });

        return (fileList, docAccepted);
    }
}
