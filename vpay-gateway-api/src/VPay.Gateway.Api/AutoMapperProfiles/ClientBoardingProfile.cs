using AutoMapper;
using VPay.Gateway.Api.Converters;
using VPay.Gateway.Common.PlatformConfig;

namespace VPay.Gateway.Api.AutoMapperProfiles;

public class ClientBoardingProfile : Profile
{
    public ClientBoardingProfile()
    {
        CreateMap<ClientConfigurationDto, ClientPrefixUserKeyConfigurationDto>(MemberList.Destination)
            .ForMember(dest => dest.IsActive, opt => opt.ConvertUsing(new ActiveToBoolConverter(), src => src.Status))
            .ForMember(dest => dest.IsEnabled, opt => opt.ConvertUsing(new YesNoToBoolConverter(), src => src.Data1));
    }
}
