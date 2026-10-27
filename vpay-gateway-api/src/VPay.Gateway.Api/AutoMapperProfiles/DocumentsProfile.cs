using AutoMapper;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Common.Documents.Models;

namespace VPay.Gateway.Api.AutoMapperProfiles;

/// <summary>
/// The profile containing mappings related to documents.
/// </summary>
public class DocumentsProfile : Profile
{
    public DocumentsProfile()
    {
        CreateMap<ArchiveDetail, DocumentDto>(MemberList.Destination)
            .ForMember(dest => dest.DocumentId, opt => opt.MapFrom(src => src.ArchiveDetailId));

        CreateMap<ArchiveDetail, FilteredDocumentDto>(MemberList.Destination)
            .ForMember(dest => dest.DocumentId, opt => opt.MapFrom(src => src.ArchiveDetailId));
    }
}
