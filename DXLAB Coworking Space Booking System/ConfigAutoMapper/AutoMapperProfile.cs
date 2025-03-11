using AutoMapper;
using DxLabCoworkingSpace;
using DXLAB_Coworking_Space_Booking_System;

namespace DXLAB_Coworking_Space_Booking_System
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Role, RoleDTO>().ReverseMap();
            CreateMap<Slot, SlotDTO>().ReverseMap();
            CreateMap<User, UserDTO>().ReverseMap();
            CreateMap<User, AccountDTO>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : ""))
            .ForMember(dest => dest.FullName, opt => opt.NullSubstitute(""))
            .ForMember(dest => dest.Email, opt => opt.NullSubstitute(""));

            CreateMap<AccountDTO, User>()
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore());
        }
    }
}