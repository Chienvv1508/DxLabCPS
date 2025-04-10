using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public StaffBookingHistoryController(IRoomService roomService, ISlotService slotService, IBookingService bookingService, IBookingDetailService bookDetailService, IAreaService areaService, IAreaTypeService areaTypeService, IMapper mapper)
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
        public async Task<IActionResult> GetAllBookingHistory()
        {
            try
            {
                // lấy tất cả booking
                var bookings = await _bookingService.GetAllWithInclude(
                    b => b.BookingDetails,
                    b => b.User
                    );

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

                if (booking == null)
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

                // Lấy tất cả AreaIds và AreaTypeIds từ filteredDetails
                var areaIds = filteredDetails
                    .Where(bd => bd.Position != null)
                    .Select(bd => bd.Position.AreaId)
                    .Distinct()
                    .ToList();

                // Lấy Areas trước, sau đó lọc
                var allAreasWithRooms = await _areaService.GetAllWithInclude(a => a.Room); // Lấy tất cả Areas với Room
                var areas = areaIds.Any() ? allAreasWithRooms.Where(a => areaIds.Contains(a.AreaId)).ToList() : new List<Area>();

                // Lấy AreaTypeIds từ cả Area trong BookingDetails và Areas từ Position
                var areaTypeIds = filteredDetails
                    .Where(bd => bd.Area?.AreaTypeId != null)
                    .Select(bd => bd.Area.AreaTypeId)
                    .Union(areas.Select(a => a.AreaTypeId))
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
                        string positionDisplay = null;
                        string areaName = null;
                        string areaTypeName = null;
                        string roomName = null;

                        if (bd.Area?.AreaId != null)
                        {
                            areaName = bd.Area.AreaName;
                            positionDisplay = areaTypeLookup.TryGetValue(bd.Area.AreaTypeId, out var areaType) ? areaType.AreaTypeName : "N/A";
                            roomName = bd.Area.Room?.RoomName;
                            areaTypeName = positionDisplay; 
                        }
                        else if (bd.Position != null)
                        {
                            positionDisplay = bd.Position.PositionNumber.ToString();
                            if (areaLookup.TryGetValue(bd.Position.AreaId, out var area))
                            {
                                areaName = area.AreaName;
                                areaTypeName = area.AreaTypeId != 0 && areaTypeLookup.TryGetValue(area.AreaTypeId, out var areaType) ? areaType.AreaTypeName : "N/A";
                                roomName = area.Room?.RoomName;
                            }
                            else
                            {
                                areaName = "N/A";
                                areaTypeName = "N/A";
                                roomName = "N/A";
                            }
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
                var bookingDate = bookingDetail.CheckoutTime.Date;
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

                return Ok(new ResponseDTO<object>(200, $"Check-out thành công cho BookingDetail {bookingDetailId}!", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi thực hiện check-out", ex.Message));
            }
        }
    }
}
