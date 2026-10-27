using System;
using System.Text.Json;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Common.Models.Sql;
using VPay.DocSys.Generation.Models;

namespace VPay.DocSys.Generation;

public class StpC2BRemittanceDocument : StpRemittanceDocumentBase
{
    public class RemittanceConsumer
    {
        public string PatientName { get; set; }
        public string PatientDOB { get; set; }
        public string ClaimId { get; set; }
        public string PatientAccountNumber { get; set; }
        public string Amount { get; set; }
        public string ClaimDate { get; set; }
        public string SubscriberName { get; set; }
        public string ClientReferenceId { get; set; }
    }

    public RemittanceConsumer Consumer { get; set; }

    public override void Create(EobXsl eob, string networkName, long transactionId,
        string trnSegment, TpaAddress tpaAddress, string networkUuid = null)
    {
        base.Create(eob, networkName, transactionId, trnSegment, tpaAddress, networkUuid);

        Consumer = new RemittanceConsumer
        {
            PatientName = eob.PatientName.Trim(),
            PatientDOB = GetFormattedDateCust(eob.PatientDOB.Trim()),
            ClaimId = eob.ClaimId.TruncateAndTrim(20),
            PatientAccountNumber = eob.PatientAccountNumber.Trim(),
            Amount = eob.Amount.Trim(),
            ClaimDate = GetFormattedDateCust(eob.ClaimDate.Trim()),
            SubscriberName = eob.SubscriberName.Trim(),
            ClientReferenceId = GetClientReferenceIdFromTrnSegment(trnSegment).TruncateAndTrim(20)
        };
    }
    
    public override string ToJsonString()
    {
        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(this, serializeOptions);
    }

    public static string GetClientReferenceIdFromTrnSegment(string trnSegment)
    {
        var values = trnSegment.Split('*');

        return values.Length > 2 ? values[2] : "";
    }

    public static string GetFormattedDateCust(string sourceDate)
    {
        try
        {
            if(sourceDate.Contains('-'))
            {
                return sourceDate;
            }
            var fDate = DateOnly.ParseExact(sourceDate, "yyyyMMdd");
            return fDate.ToString("yyyy-MM-dd");
        }
        catch (FormatException)
        {
            return "";
        }
    }

}
