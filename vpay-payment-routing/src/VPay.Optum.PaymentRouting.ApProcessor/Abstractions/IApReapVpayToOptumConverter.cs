using System.Threading.Tasks;
using VPay.Optum.Http.ConfigApi.Models;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Common.Serialization;

namespace VPay.Optum.PaymentRouting.ApProcessor.Abstractions
{
    public interface IApReapVPayToOptumConverter
    {
        Task<VpayApReap> ConvertApReapMessage(ApReapMessage message);

        Task<PrsMappingDto> GetPrsMapping(string tpaCode, string billingEntity);
    }
}
