using AutoMapper;
using DxLabCoworkingSpace;
using DXLAB_Coworking_Space_Booking_System;

namespace DXLAB_Coworking_Space_Booking_System
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Mapping cho Role
            CreateMap<Role, RoleDTO>().ReverseMap();

            // Mapping cho Slot
            CreateMap<Slot, SlotDTO>().ReverseMap();

            // Mapping cho User
            CreateMap<User, UserDTO>().ReverseMap();

            // Mapping cho AccountDTO
            CreateMap<User, AccountDTO>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : ""))
                .ForMember(dest => dest.FullName, opt => opt.NullSubstitute(""))
                .ForMember(dest => dest.Email, opt => opt.NullSubstitute(""));
            CreateMap<AccountDTO, User>()
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore());

            // Mapping cho Facilities
            CreateMap<Facility, FacilitiesDTO>().ReverseMap();

            // Mapping cho Blog
            CreateMap<Blog, BlogDTO>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (BlogDTO.BlogStatus)src.Status))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.BlogCreatedDate, opt => opt.MapFrom(src => src.BlogCreatedDate.ToString("yyyy-MM-ddTHH:mm:ss")))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images != null
                    ? src.Images.Where(i => i.BlogId != null)
                        .Select(i => i.ImageUrl)
                        .ToList()
                    : new List<string>()))
                .ForMember(dest => dest.ImageFiles, opt => opt.Ignore());

            CreateMap<BlogDTO, Blog>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (int)src.Status))
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.BlogCreatedDate, opt => opt.Ignore()) // Bỏ qua ánh xạ BlogCreatedDate
                .ForMember(dest => dest.Images, opt => opt.Ignore());

            CreateMap<BlogRequestDTO, Blog>()
                .ForMember(dest => dest.BlogId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.BlogCreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.Images, opt => opt.Ignore());

            CreateMap<RoomDTO, Room>()
           .ForMember(dest => dest.Images, opt => opt.MapFrom(src =>
               src.Images != null ? src.Images.Select(url => new Image { ImageUrl = url }).ToList() : null))
           .ForMember(dest => dest.Areas, opt => opt.MapFrom(x => x.Area_DTO != null ? x.Area_DTO.Select(a => new Area { AreaName = a.AreaName, AreaTypeId = a.AreaTypeId }) : null));

            CreateMap<Room, RoomDTO>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src =>
                    src.Images != null ? src.Images.Select(i => i.ImageUrl).ToList() : null))
                .ForMember(dest => dest.Area_DTO, opt => opt.MapFrom(x => x.Areas != null ? x.Areas.Select(a => new AreaDTO { AreaName = a.AreaName, AreaTypeId = a.AreaTypeId }) : null));

            CreateMap<AreaTypeDTO, AreaType>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src =>
             src.Images != null ? src.Images.Select(url => new Image { ImageUrl = url }).ToList() : null));

            CreateMap<AreaType, AreaTypeDTO>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src =>
                    src.Images != null ? src.Images.Select(i => i.ImageUrl).ToList() : null));
            CreateMap<AreaType, AreaDTO>().ReverseMap();
            CreateMap<Area, AreaDTO>().ReverseMap();

        }
    }
}