using AutoMapper;
using VPay.Gateway.Data.Sql.PepClaims;
using VPay.Gateway.Management.Api.Models;

namespace VPay.Gateway.Management.Api.AutoMapperProfiles;

public class GuidewireEnrollmentProfile : Profile
{
    public GuidewireEnrollmentProfile()
    {
        CreateMap<EnrollmentStatus, GuidewireEnrollmentStatusDto>(MemberList.Destination)
            .ForMember(dest => dest.PayeeStatusId, src => src.MapFrom(x => x.Id))
            .ForMember(dest => dest.ProcessStatus, src => src.MapFrom(x => x.CurrentProcessStatus));
    }
}
