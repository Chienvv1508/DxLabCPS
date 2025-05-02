using AutoMapper;
using DXLAB_Coworking_Space_Booking_System.Hubs;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/bookinghistory")]
    [ApiController]
    [Authorize(Roles = "Staff")]
    public class StaffBookingHistoryController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly ISlotService _slotService;
        private readonly IBookingService _bookingService;
        private readonly IBookingDetailService _bookDetailService;
        private readonly IAreaService _areaService;
        private readonly IAreaTypeService _areaTypeService;
        private readonly IMapper _mapper;
        private readonly IHubContext<BookingHistoryHub> _hubContext;
        public StaffBookingHistoryController(IRoomService roomService, ISlotService slotService, IBookingService bookingService, IBookingDetailService bookDetailService, IAreaService areaService, IAreaTypeService areaTypeService, IMapper mapper, IHubContext<BookingHistoryHub> hubContext)
        {
            _roomService = roomService;
            _slotService = slotService;
            _bookingService = bookingService;
            _bookDetailService = bookDetailService;
            _areaService = areaService;
            _areaTypeService = areaTypeService;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBookingHistory()
        {
            try
            {
                // lấy tất cả booking
                var bookings = await _bookingService.GetAllWithInclude(
                    b => b.BookingDetails,
                    b => b.User
                    );
                bookings = bookings.Where(x => x.BookingDetails.Any() == true);
                if (!bookings.Any())
                {
                    return Ok(new ResponseDTO<object>(200, "Không có lịch sử booking nào!", null));
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

                var response = new ResponseDTO<object>(200, "Lấy danh sách lịch sử booking cho Staff thành công!", bookingHistoryList);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(500, "Lỗi khi lấy danh sách lịch sử booking", ex.Message);
                return StatusCode(500, response);
            }
        }

        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetStaffBookingHistoryDetail(int bookingId)
        {
            try
            {
                // Lấy Booking
                var booking = await _bookingService.GetWithInclude(
                    b => b.BookingId == bookingId,
                    b => b.BookingDetails,
                    b => b.User
                );

                if (booking == null || booking.BookingDetails.Any() == false)
                {
                    return NotFound(new ResponseDTO<object>(404, "Không tìm thấy booking!", null));
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
                    .Select(bd => bd.Position != null ? bd.Position.AreaId : (bd.Area != null ? bd.Area.AreaId : 0)) // Lấy AreaId từ Position hoặc Area, mặc định 0 nếu không có
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
                            Status = bd.Status
                        };
                    }).ToList()
                };

                var response = new ResponseDTO<object>(200, "Lấy chi tiết booking cho Staff thành công!", responseData);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(500, "Lỗi khi lấy chi tiết booking", ex.Message);
                return StatusCode(500, response);
            }
        }

        [HttpPost("checkin/{bookingDetailId}")]
        public async Task<IActionResult> CheckIn(int bookingDetailId)
        {
            try
            {
                var bookingDetail = await _bookDetailService.GetWithInclude(
                    bd => bd.BookingDetailId == bookingDetailId,
                    bd => bd.Slot,
                    bd => bd.Booking
                );

                if (bookingDetail == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Không tìm thấy BookingDetail với ID {bookingDetailId}!", null));
                }

                if (bookingDetail.Status == 1) // 1 = CheckedIn
                {
                    return BadRequest(new ResponseDTO<object>(400, "Booking này đã được check-in!", null));
                }

                if (bookingDetail.Status == 2) // 2 = Completed
                {
                    return BadRequest(new ResponseDTO<object>(400, "Booking này đã hoàn thành, không thể check-in!", null));
                }

                var currentTime = DateTime.Now;
                var slotEndTime = bookingDetail.Slot?.EndTime ?? TimeSpan.Zero; // Xử lý null cho TimeSpan?
                var bookingDate = bookingDetail.CheckinTime.Date;
                var slotEndDateTime = bookingDate + slotEndTime;

                if (currentTime < bookingDetail.CheckinTime)
                {
                    return BadRequest(new ResponseDTO<object>(400, $"Chưa đến thời gian check-in ({bookingDetail.CheckinTime})!", null));
                }

                if (currentTime >= bookingDetail.CheckoutTime)
                {
                    return BadRequest(new ResponseDTO<object>(400, $"Đã quá thời gian check-in, hiện tại là thời gian check-out hoặc nghỉ ({bookingDetail.CheckoutTime} - {slotEndDateTime})!", null));
                }

                await _bookDetailService.UpdateStatus(bookingDetailId, 1); // 1 = CheckedIn

                // Chuẩn bị dữ liệu thông báo real-time
                var area = bookingDetail.Position != null ? bookingDetail.Position.Area : bookingDetail.Area;
                var areaType = area != null ? await _areaTypeService.Get(at => at.AreaTypeId == area.AreaTypeId) : null;
                var bookingDetailData = new
                {
                    BookingDetailId = bookingDetail.BookingDetailId,
                    BookingId = bookingDetail.BookingId,
                    Position = bookingDetail.Position?.PositionNumber.ToString() ?? areaType?.AreaTypeName ?? "N/A",
                    AreaName = area?.AreaName ?? "N/A",
                    AreaTypeName = areaType?.AreaTypeName ?? "N/A",
                    RoomName = area?.Room?.RoomName ?? "N/A",
                    SlotNumber = bookingDetail.Slot?.SlotNumber,
                    CheckinTime = bookingDetail.CheckinTime,
                    CheckoutTime = bookingDetail.CheckoutTime,
                    Status = 1, 
                    UserId = bookingDetail.Booking.UserId
                };

                // Gửi thông báo real-time tới Student và Staff
                await _hubContext.Clients.Group("Student").SendAsync("ReceiveBookingStatus", bookingDetailData);
                await _hubContext.Clients.Group("Staff").SendAsync("ReceiveBookingStatus", bookingDetailData);

                return Ok(new ResponseDTO<object>(200, $"Check-in thành công cho BookingDetail {bookingDetailId}!", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi thực hiện check-in", ex.Message));
            }
        }

        [HttpPost("checkout/{bookingDetailId}")]
        public async Task<IActionResult> CheckOut(int bookingDetailId)
        {
            try
            {
                var bookingDetail = await _bookDetailService.GetWithInclude(
                    bd => bd.BookingDetailId == bookingDetailId,
                    bd => bd.Slot,
                    bd => bd.Booking
                );

                if (bookingDetail == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Không tìm thấy BookingDetail với ID {bookingDetailId}!", null));
                }

                if (bookingDetail.Status == 0) // 0 = Pending
                {
                    return BadRequest(new ResponseDTO<object>(400, "Booking này chưa được check-in, không thể check-out!", null));
                }

                if (bookingDetail.Status == 2) // 2 = Completed
                {
                    return BadRequest(new ResponseDTO<object>(400, "Booking này đã hoàn thành!", null));
                }

                var currentTime = DateTime.Now;
                var slotEndTime = bookingDetail.Slot?.EndTime ?? TimeSpan.Zero; // Xử lý null cho TimeSpan?
                var bookingDate = bookingDetail.CheckoutTime.Value.Date;
                var slotEndDateTime = bookingDate + slotEndTime;

                if (currentTime < bookingDetail.CheckoutTime)
                {
                    return BadRequest(new ResponseDTO<object>(400, $"Chưa đến thời gian check-out ({bookingDetail.CheckoutTime})!", null));
                }

                //if (currentTime > slotEndDateTime)
                //{
                //    return BadRequest(new ResponseDTO<object>(400, $"Đã quá thời gian check-out, hiện tại là thời gian nghỉ ({slotEndDateTime})!", null));
                //}

                await _bookDetailService.UpdateStatus(bookingDetailId, 2); // 2 = Completed

                // Chuẩn bị dữ liệu thông báo real-time
                var area = bookingDetail.Position != null ? bookingDetail.Position.Area : bookingDetail.Area;
                var areaType = area != null ? await _areaTypeService.Get(at => at.AreaTypeId == area.AreaTypeId) : null;
                var bookingDetailData = new
                {
                    BookingDetailId = bookingDetail.BookingDetailId,
                    BookingId = bookingDetail.BookingId,
                    Position = bookingDetail.Position?.PositionNumber.ToString() ?? areaType?.AreaTypeName ?? "N/A",
                    AreaName = area?.AreaName ?? "N/A",
                    AreaTypeName = areaType?.AreaTypeName ?? "N/A",
                    RoomName = area?.Room?.RoomName ?? "N/A",
                    SlotNumber = bookingDetail.Slot?.SlotNumber,
                    CheckinTime = bookingDetail.CheckinTime,
                    CheckoutTime = bookingDetail.CheckoutTime,
                    Status = 2, 
                    UserId = bookingDetail.Booking.UserId
                };

                // Gửi thông báo real-time tới Student và Staff
                await _hubContext.Clients.Group("Student").SendAsync("ReceiveBookingStatus", bookingDetailData);
                await _hubContext.Clients.Group("Staff").SendAsync("ReceiveBookingStatus", bookingDetailData);

                return Ok(new ResponseDTO<object>(200, $"Check-out thành công cho BookingDetail {bookingDetailId}!", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi thực hiện check-out", ex.Message));
            }
        }
    }
}
