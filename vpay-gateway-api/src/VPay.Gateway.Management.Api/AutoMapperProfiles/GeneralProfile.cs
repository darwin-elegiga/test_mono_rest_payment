using AutoMapper;
using VPay.Gateway.Data.Paging;

namespace VPay.Gateway.Management.Api.AutoMapperProfiles;

public class GeneralProfile : Profile
{
    public GeneralProfile()
    {
        CreateMap(typeof(PagedResult<>), typeof(Pagination<>), MemberList.Destination);
    }
}
