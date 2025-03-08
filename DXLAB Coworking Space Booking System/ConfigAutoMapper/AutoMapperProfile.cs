using AutoMapper;
using DxLabCoworkingSpace;


namespace DXLAB_Coworking_Space_Booking_System
{
    public class AutoMapperProfile: Profile
    {
        public AutoMapperProfile() 
        {
            CreateMap<Role, RoleDto>().ReverseMap();
        }
       
    }
}
