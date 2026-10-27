using AutoMapper;
using VPay.Gateway.Common.Commands;
using VPay.Gateway.Common.Models;
using VPay.Gateway.Management.Api.Models;

namespace VPay.Gateway.Management.Api.AutoMapperProfiles;

public class GuidewireProfile : Profile
{
    public GuidewireProfile()
    {
        CreateMap<GuidewireEnrollmentStatusChangeDto, SendGuidewireMessage>()
            .ForMember(dest => dest.CallType, opt => opt.MapFrom(_ => GatewayCallType.EnrollmentStatus))
            .ForMember(dest => dest.PaymentId, opt => opt.Ignore());

        CreateMap<GuidewireNoteOnlyDto, SendGuidewireMessage>()
            .ForMember(dest => dest.CallType, opt => opt.MapFrom(_ => GatewayCallType.NoteOnly))
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Note, opt => opt.MapFrom(x => x));

        CreateMap<GuidewireNoteDto, GuidewireNote>()
            .ForMember(dest => dest.Topic, opt => opt.MapFrom(src => src.Topic.ToString().ToLowerInvariant()));
    }
}
