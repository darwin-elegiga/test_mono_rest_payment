using System;
using System.Threading.Tasks;
using VPay.Extensions.Exceptions;
using VPay.Optum.Http.ConfigApi;
using VPay.Optum.Http.ConfigApi.Models;
using VPay.Optum.PaymentRouting.ApProcessor.Abstractions;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Common.Serialization;

namespace VPay.Optum.PaymentRouting.ApProcessor.Services
{
    public class ApReapVPayToOptumConverter : IApReapVPayToOptumConverter
    {
        private readonly IConfigApi _configApi;
        private readonly int _expectedTrnSegments = 5;

        public ApReapVPayToOptumConverter(
            IConfigApi configApi)
        {
            _configApi = configApi;
        }

        public async Task<VpayApReap> ConvertApReapMessage(ApReapMessage message)
        {
            var result = new VpayApReap();

            var trnSegments = message.TrnSegment.Split("*");
            if (trnSegments.Length != _expectedTrnSegments)
            {
                throw new BadRequestException($"Expected 5 TRN segments but found {trnSegments.Length}");
            }

            var prsMapping = await GetPrsMapping(message.TpaCode, message.BillingEntity);
            result.payr835ID = prsMapping.OptumPayorId;
            result.payerSubpayerID = prsMapping.OptumSubPayorId;

            result.payMethodCode = PayMethodCodeConverter(message.PaymentType);

            result.paymentSource = "VP";
            result.paymentType = PaymentTypeConverter(message.MessageType);
            result.providerNumber = message.ProviderTin;
            result.providerName = message.ProviderName;
            result.provNPINumber = message.ProviderNPPID;
            result.unConsolPayNumber = message.UserKey;
            result.unConsolPayAmount = message.LoadAmount.ToString();
            result.displConsolPayNumber = trnSegments[2];
            result.consolAmount = message.LoadAmount.ToString();
            result.settlementDate = message.LoadDate.AddDays(1).ToString("yyyyMMdd");

            if (result.paymentType == "RP")
            {
                result.checkNumber = message.ReapCheckNumber;
                result.revisedMethodCode = PayMethodCodeConverter(message.ReapPaymentType);
                result.reoriginatedDate = message.ReapLoadDate.GetValueOrDefault(DateTime.Today).AddDays(1).ToString("yyyyMMdd");
                result.returnReasonCode = ReturnReasonCodeConverter(result.payMethodCode);
            }

            return result;
        }

        public async Task<PrsMappingDto> GetPrsMapping(string tpaCode, string billingEntity)
        {
            return await _configApi.GetPrsMapping(tpaCode, billingEntity).ConfigureAwait(false);
        }

        internal static string ReturnReasonCodeConverter(string payMethodCode)
        {
            return payMethodCode switch
            {
                "CHK" or "VCP" => "EXP",
                "ACH" => "R01",
                _ => string.Empty
            };
        }

        internal static string PayMethodCodeConverter(string paymentType)
        {
            if (string.IsNullOrEmpty(paymentType))
            {
                return string.Empty;
            }

            return paymentType switch
            {
                "CLEFT" or "CLSDA" or "OPACH" or "CLCFT" => "ACH",
                "CLCHK" => "CHK",
                "CLNPF" => "NON",
                "CLVCP" => "VCP",
                _ => "",
            };
        }

        internal static string PaymentTypeConverter(string messageType)
        {
            return messageType switch
            {
                "AP" => "AP",
                "RP" => "RP",
                _ => ""
            };
        }
    }
}
