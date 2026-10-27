using AutoMapper;
using VPay.Gateway.Data.Sql.GatewayClients;
using VPay.Gateway.Management.Api.Models;

namespace VPay.Gateway.Management.Api.AutoMapperProfiles;

public class GatewayClientProfile : Profile
{
    public GatewayClientProfile()
    {
        CreateMap<GatewayClient, GatewayClientDto>(MemberList.Destination)
            .ForMember(dest => dest.IsExpired, src => src.MapFrom(x => x.ExpiredDateTime.HasValue));

        CreateMap<GatewayClient, GatewayClientCreateResult>(MemberList.Destination)
            .ForMember(dest => dest.IsExpired, src => src.MapFrom(x => x.ExpiredDateTime.HasValue))
            .ForMember(dest => dest.ClientSecret, src => src.Ignore());
    }
}
