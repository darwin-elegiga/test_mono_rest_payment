using AutoMapper;
using VPay.Gateway.Data.Sql.Payments;
using VPay.Gateway.Management.Api.Models;

namespace VPay.Gateway.Management.Api.AutoMapperProfiles;

public class PaymentsProfile : Profile
{
    public PaymentsProfile()
    {
        CreateMap<Application.GuidewirePayments.GetPaymentStatusTrackingResult, GuidewirePaymentStatusResult>(MemberList.Destination)
            .ForMember(dest => dest.ClientCode, src => src.MapFrom(x => x.Payment.ClientCode))
            .ForMember(dest => dest.ClientPaymentId, src => src.MapFrom(x => x.Payment.ClientPaymentId))
            .ForMember(dest => dest.CurrentStatus, src => src.MapFrom(x => x.Payment.LastSentStatus))
            .ForMember(dest => dest.Statuses, src => src.MapFrom(x => x.StatusTrackings));

        CreateMap<PaymentStatusTracking, GuidewirePaymentStatusTrackingResult>(MemberList.Destination)
            .ForMember(dest => dest.SentDateTime, src => src.MapFrom(x => x.CreatedDateTime));
    }
}
