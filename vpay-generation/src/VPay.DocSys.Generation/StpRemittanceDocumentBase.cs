using System;
using System.Text.Json.Serialization;
using System.Xml.XPath;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Common.Models.Sql;
using VPay.DocSys.Generation.Models;

namespace VPay.DocSys.Generation;

public abstract class StpRemittanceDocumentBase
{
    public class RemittanceHeader
    {
        public string NetworkPartner { get; set; }
        public string OptumTransactionId { get; set; }
    }

    public class RemittancePayee
    {
        public string Name { get; set; }
        public string Tin { get; set; }
        public string AddressLine1 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Uuid { get; set; }
    }

    public class RemittancePayer
    {
        public string TraceNumber { get; set; }
        public string Tpa { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
    }

    [JsonPropertyOrder(-1)]
    public RemittanceHeader Header { get; set; }

    [JsonPropertyOrder(1)]
    public RemittancePayee Payee { get; set; }

    [JsonPropertyOrder(2)]
    public RemittancePayer Payer { get; set; }

    public virtual void Create(EobXsl eob, string networkName, long transactionId, string trnSegment, TpaAddress tpaAddress, string networkUuid = null)
    {
        Header = new()
        {
            NetworkPartner = networkName.Trim(),
            OptumTransactionId = transactionId.ToString()
        };

        Payee = new()
        {
            Name = eob.PayeeName.TruncateAndTrim(30),
            Tin = eob.MainGovtIdNumber.TruncateAndTrim(10),
            AddressLine1 = eob.PayeeAddress1.TruncateAndTrim(30),
            City = eob.PayeeCity.TruncateAndTrim(30),
            State = eob.PayeeState.TruncateAndTrim(20),
            Zip = eob.PayeePostalCode.TruncateAndTrim(20),
            Uuid = networkUuid
        };

        Payer = new()
        {
            TraceNumber = trnSegment.Trim(),
            Tpa = tpaAddress.Tpa,
            Address = tpaAddress.Address,
            City = tpaAddress.City,
            State = tpaAddress.State,
            Zip = tpaAddress.Zip
        };
    }

    public abstract string ToJsonString();

    protected static string GetEobXmlValue(XPathNavigator nav, string xpath, int maxLength)
    {
        try
        {
            var value = nav.SelectSingleNode(xpath).Value;
            return value.PadRight(maxLength)[..maxLength].Trim();
        }
        catch (Exception)
        {
            return "";
        }
    }

    protected static string GetFormattedDate(string sourceDate)
    {
        try
        {
            var fDate = DateOnly.ParseExact(sourceDate, "yyyyMMdd");
            return fDate.ToString("yyyy-MM-dd");
        }
        catch (FormatException)
        {
            return "";
        }
    }
}
