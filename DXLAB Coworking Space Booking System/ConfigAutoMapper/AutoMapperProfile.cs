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
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images != null ? src.Images.Select(url => new Image { ImageUrl = url }).ToList() : null))
                .ForMember(dest => dest.Areas, opt => opt.MapFrom(x => x.Area_DTO != null ? x.Area_DTO.Select(a => new Area
                    {
                        AreaName = a.AreaName,
                        AreaTypeId = a.AreaTypeId,
                        Images = a.Images != null ? a.Images.Select(url => new Image { ImageUrl = url }).ToList() : null
                    }).ToList() : null));

            CreateMap<RoomDTO, Room>()
    .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images != null ? src.Images.Select(url => new Image { ImageUrl = url }).ToList() : null))
    .ForMember(dest => dest.Areas, opt => opt.MapFrom(x => x.Area_DTO != null ? x.Area_DTO.Select(a => new Area
    {
        AreaName = a.AreaName,
        AreaTypeId = a.AreaTypeId,
        AreaDescription = a.AreaDescription, // Thêm ánh xạ AreaDescription
        Images = a.Images != null ? a.Images.Select(url => new Image { ImageUrl = url }).ToList() : null
    }).ToList() : null));

            CreateMap<Room, RoomDTO>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images != null ? src.Images.Select(i => i.ImageUrl).ToList() : null))
                .ForMember(dest => dest.Area_DTO, opt => opt.MapFrom(x => x.Areas != null ? x.Areas.Select(a => new AreaDTO
                {
                    AreaId = a.AreaId,
                    AreaName = a.AreaName,
                    AreaTypeId = a.AreaTypeId,
                    AreaTypeName = a.AreaType != null ? a.AreaType.AreaTypeName : null,
                    AreaDescription = a.AreaDescription, // Thêm ánh xạ AreaDescription
                    Images = a.Images != null ? a.Images.Select(i => i.ImageUrl).ToList() : null // Thêm ánh xạ Images
                }).ToList() : null));

            CreateMap<AreaTypeDTO, AreaType>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images != null ? src.Images.Select(url => new Image { ImageUrl = url }).ToList() : null));

            CreateMap<AreaType, AreaTypeDTO>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images != null ? src.Images.Select(i => i.ImageUrl).ToList() : null));

            // Sửa ánh xạ AreaDTO -> Area
            CreateMap<AreaDTO, Area>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images != null ? src.Images.Select(url => new Image { ImageUrl = url }).ToList() : null))
                .ForMember(dest => dest.BookingDetails, opt => opt.Ignore())
                .ForMember(dest => dest.Positions, opt => opt.Ignore())
                .ForMember(dest => dest.UsingFacilities, opt => opt.Ignore())
                .ForMember(dest => dest.AreaType, opt => opt.Ignore())
                .ForMember(dest => dest.Room, opt => opt.Ignore());

            CreateMap<Area, AreaDTO>()
                .ForMember(dest => dest.AreaTypeName, opt => opt.MapFrom(x => x.AreaType != null ? x.AreaType.AreaTypeName : null))
                .ForMember(dest => dest.AreaDescription, opt => opt.MapFrom(src => src.AreaDescription)) // Thêm ánh xạ AreaDescription
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images != null ? src.Images.Select(i => i.ImageUrl).ToList() : null));
            
            // Mapping cho thống kê
            CreateMap<Booking, StudentRevenueDTO>();
            CreateMap<BookingDetail, ServiceTypeDetailDTO>()
                .ForMember(dest => dest.ServiceType, opt => opt.MapFrom(src => src.Area.AreaType.AreaTypeName));
            CreateMap<BookingDetail, RoomPerformanceDTO>()
                .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Area.Room.RoomName));
            CreateMap<BookingDetail, RoomServicePerformanceDTO>()
                .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Area.Room.RoomName))
                .ForMember(dest => dest.ServiceType, opt => opt.MapFrom(src => src.Area.AreaType.AreaTypeName));

            CreateMap<FacilitiesStatus, FaciStatusDTO>()
                .ForMember(d => d.FacilityName, opt => opt.MapFrom(s => s.Facility.FacilityTitle));
            CreateMap<Area, AreaGetDTO>()
                .ForMember(x => x.AreaId, opt => opt.MapFrom(s => s.AreaId))
                .ForMember(x => x.AreaName, opt => opt.MapFrom(s => s.AreaName));
            CreateMap<UsingFacility, FaciGetInAreaDTO>()
                .ForMember(x => x.FacilityId, opt => opt.MapFrom(s => s.FacilityId))
                .ForMember(x => x.FacilityTitle, opt => opt.MapFrom(s => s.Facility.FacilityTitle));

            CreateMap<AreaTypeCategoryDTO, AreaTypeCategory>()
                .ForMember(x => x.Images, opt => 
                    opt.MapFrom(src => src.Images != null ? src.Images.Select(url => new Image { ImageUrl = url }).ToList() : null));
            CreateMap<AreaTypeCategory, AreaTypeCategoryDTO>()
               .ForMember(x => x.Images, opt =>
                   opt.MapFrom(src => src.Images != null ? src.Images.Select(x => x.ImageUrl).ToList() : null));
            CreateMap<AreaTypeCategoryForAddDTO, AreaTypeCategory>()
                .ForMember(x => x.Images, opt =>
                    opt.Ignore());
            CreateMap<AreaTypeCategoryForUpdateDTO, AreaTypeCategory>().ForMember(dest => dest.Images, opt => opt.Ignore()); ;

            // Mapping cho ReportRequestDTO -> Report
            CreateMap<ReportRequestDTO, Report>()
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.UserId, opt => opt.Ignore());

            // Mapping cho Report -> ReportResponseDTO
            CreateMap<Report, ReportResponseDTO>()
    .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ss")))
    .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "N/A"))
    .ForMember(dest => dest.BookingDetailId, opt => opt.MapFrom(src => src.BookingDetailId))
    .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.BookingDetail != null && src.BookingDetail.Position != null ? src.BookingDetail.Position.PositionNumber.ToString() : "N/A"))
    .ForMember(dest => dest.AreaName, opt => opt.MapFrom(src => src.BookingDetail != null && src.BookingDetail.Area != null ? src.BookingDetail.Area.AreaName : "N/A"))
    .ForMember(dest => dest.AreaTypeName, opt => opt.MapFrom(src => src.BookingDetail != null && src.BookingDetail.Area != null && src.BookingDetail.Area.AreaType != null ? src.BookingDetail.Area.AreaType.AreaTypeName : "N/A"))
    .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.BookingDetail != null && src.BookingDetail.Area != null && src.BookingDetail.Area.Room != null ? src.BookingDetail.Area.Room.RoomName : "N/A"));
        }
    }
}