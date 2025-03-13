using AutoMapper;
using DxLabCoworkingSpace;
using DXLAB_Coworking_Space_Booking_System;
using DxLabCoworkingSpace.Core.DTOs;

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

            // Mapping cho Blog và BlogDTO
            CreateMap<Blog, BlogDTO>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (BlogDTO.BlogStatus)src.Status)) // int -> enum
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images
                    .Where(i => i.BlogId != null)
                    .Select(i => i.ImageUrl)
                    .ToList()));
            CreateMap<BlogDTO, Blog>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (int)src.Status)) // enum -> int
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images != null
                    ? src.Images.Select(url => new Image
                    {
                        ImageUrl = url,
                        BlogId = src.BlogId
                    }).ToList()
                    : new List<Image>()));
        }
    }
}