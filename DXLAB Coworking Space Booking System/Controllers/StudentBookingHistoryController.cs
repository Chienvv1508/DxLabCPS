using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/studentbookinghistory")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class StudentBookingHistoryController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly ISlotService _slotService;
        private readonly IBookingService _bookingService;
        private readonly IBookingDetailService _bookDetailService;
        private readonly IAreaService _areaService;
        private readonly IAreaTypeService _areaTypeService;
        private readonly IMapper _mapper;
        public StudentBookingHistoryController(IRoomService roomService, ISlotService slotService, IBookingService bookingService, IBookingDetailService bookDetailService, IAreaService areaService, IAreaTypeService areaTypeService, IMapper mapper)
        {
            _roomService = roomService;
            _slotService = slotService;
            _bookingService = bookingService;
            _bookDetailService = bookDetailService;
            _areaService = areaService;
            _areaTypeService = areaTypeService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentBookingHistory()
        {
            try
            {
                // Lấy UserId từ claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
                }

                // lấy tất cả booking
                var allBookings = await _bookingService.GetAllWithInclude(
                    b => b.BookingDetails,
                    b => b.User
                    );

                // Lọc booking theo UserId trong bộ nhớ
                var bookings = allBookings.Where(b => b.UserId == userId && b.BookingDetails.Any() == true).ToList();
                

                if (!bookings.Any())
                {
                    return Ok(new ResponseDTO<object>(200, "Bạn không có lịch sử booking nào!", null));
                }



                var bookingHistoryList = bookings.Select(b => new
                {
                    BookingId = b.BookingId,
                    UserName = b.User?.FullName,
                    UserEmail = b.User?.Email,
                    BookingCreatedDate = b.BookingCreatedDate,
                    TotalPrice = b.Price,
                    TotalBookingDetail = b.BookingDetails.Count,
                }).ToList();

                var response = new ResponseDTO<object>(200, "Lấy danh sách lịch sử booking của bạn thành công!", bookingHistoryList);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(500, "Lỗi khi lấy danh sách lịch sử booking", ex.Message);
                return StatusCode(500, response);
            }
        }

        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetStudentBookingHistoryDetail(int bookingId)
        {
            try
            {
                // Lấy UserId từ claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
                }

                // Lấy tất cả booking với các liên kết
                var allBookings = await _bookingService.GetAllWithInclude(
                    b => b.BookingDetails,
                    b => b.User
                );

                // Lọc booking theo BookingId và UserId trong bộ nhớ
                var booking = allBookings.FirstOrDefault(b => b.BookingId == bookingId && b.UserId == userId);

                if (booking == null || booking.BookingDetails.Any() == false)
                {
                    return NotFound(new ResponseDTO<object>(404, "Không tìm thấy booking hoặc bạn không có quyền xem!", null));
                }

                // Lấy BookingDetails với các liên kết cần thiết
                var bookingDetails = await _bookDetailService.GetAllWithInclude(
                    bd => bd.Slot,
                    bd => bd.Position,
                    bd => bd.Area,
                    bd => bd.Area.Room
                );

                var filteredDetails = bookingDetails.Where(bd => bd.BookingId == bookingId).ToList();

                if (!filteredDetails.Any())
                {
                    return NotFound(new ResponseDTO<object>(404, "Không tìm thấy chi tiết booking!", null));
                }

                // Lấy tất cả AreaIds từ filteredDetails
                var areaIds = filteredDetails
                    .Select(bd => bd.Position != null ? bd.Position.AreaId : (bd.Area != null ? bd.Area.AreaId : 0)) // Lấy AreaId từ Position hoặc Area
                    .Where(id => id != 0) // Loại bỏ các id không hợp lệ
                    .Distinct()
                    .ToList();

                // Lấy Areas trước, sau đó lọc
                var allAreasWithRooms = await _areaService.GetAllWithInclude(a => a.Room);
                var areas = areaIds.Any() ? allAreasWithRooms.Where(a => areaIds.Contains(a.AreaId)).ToList() : new List<Area>();

                // Lấy AreaTypeIds từ Areas
                var areaTypeIds = areas
                    .Select(a => a.AreaTypeId)
                    .Distinct()
                    .ToList();

                // Lấy dữ liệu AreaTypes
                var areaTypes = areaTypeIds.Any() ? await _areaTypeService.GetAll(at => areaTypeIds.Contains(at.AreaTypeId)) : new List<AreaType>();

                // Tạo lookup để tra cứu nhanh
                var areaLookup = areas.ToDictionary(a => a.AreaId, a => a);
                var areaTypeLookup = areaTypes.ToDictionary(at => at.AreaTypeId, at => at);

                // Chuẩn bị dữ liệu trả về
                var responseData = new
                {
                    BookingId = booking.BookingId,
                    UserName = booking.User?.FullName,
                    UserEmail = booking.User?.Email,
                    BookingCreatedDate = booking.BookingCreatedDate,
                    TotalPrice = booking.Price,
                    Details = filteredDetails.Select(bd =>
                    {
                        string positionDisplay = "N/A";
                        string areaName = "N/A";
                        string areaTypeName = "N/A";
                        string roomName = "N/A";

                        // Lấy thông tin Area và AreaType
                        Area area = null;
                        if (bd.Position != null && areaLookup.TryGetValue(bd.Position.AreaId, out var areaFromPosition))
                        {
                            area = areaFromPosition;
                        }
                        else if (bd.Area != null && areaLookup.TryGetValue(bd.Area.AreaId, out var areaFromArea))
                        {
                            area = areaFromArea;
                        }

                        if (area != null)
                        {
                            areaName = area.AreaName;
                            areaTypeName = areaTypeLookup.TryGetValue(area.AreaTypeId, out var areaType) ? areaType.AreaTypeName : "N/A";
                            roomName = area.Room?.RoomName ?? "N/A";
                        }

                        // Xác định Position
                        if (bd.Position != null)
                        {
                            positionDisplay = bd.Position.PositionNumber.ToString(); // Hiển thị PositionNumber nếu có Position
                        }
                        else
                        {
                            positionDisplay = areaTypeName; // Hiển thị AreaTypeName nếu Position là null
                        }

                        return new
                        {
                            BookingDetailId = bd.BookingDetailId,
                            Position = positionDisplay,
                            AreaName = areaName,
                            AreaTypeName = areaTypeName,
                            RoomName = roomName,
                            SlotNumber = bd.Slot?.SlotNumber,
                            CheckinTime = bd.CheckinTime,
                            CheckoutTime = bd.CheckoutTime,
                            Price = bd.Price,
                            Status = bd.Status
                        };
                    }).ToList()
                };

                var response = new ResponseDTO<object>(200, "Lấy chi tiết booking của bạn thành công!", responseData);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                var response = new ResponseDTO<object>(500, "Lỗi khi lấy chi tiết booking", ex.Message);
                return StatusCode(500, response);
            }
        }
    }
}
