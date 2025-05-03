using AutoMapper;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System
{
    [Route("api/booking")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class BookingController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly ISlotService _slotService;
        private readonly IBookingService _bookingService;
        private readonly IBookingDetailService _bookDetailService;
        private readonly IAreaService _areaService;
        private readonly IAreaTypeService _areaTypeService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IAreaTypeCategoryService _areaTypeCategoryService;
        private readonly IBlockchainBookingService _blockchainBookingService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        public BookingController(IRoomService roomService, ISlotService slotService, IBookingService bookingService, IBookingDetailService bookDetailService,
            IAreaService areaService, IAreaTypeService areaTypeService, IMapper mapper, IConfiguration configuration, IAreaTypeCategoryService areaTypeCategoryService,
            IBlockchainBookingService blockchainBookingService, IUserService userService, IUnitOfWork unitOfWork)
        {
            _roomService = roomService;
            _slotService = slotService;
            _bookingService = bookingService;
            _bookDetailService = bookDetailService;
            _areaService = areaService;
            _areaTypeService = areaTypeService;
            _mapper = mapper;
            _configuration = configuration;
            _areaTypeCategoryService = areaTypeCategoryService;
            _blockchainBookingService = blockchainBookingService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] BookingDTO bookingDTO)
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
                }

                // Lấy WalletAddress của user
                var user = await _userService.Get(u => u.UserId == userId);
                if (user == null || string.IsNullOrEmpty(user.WalletAddress))
                {
                    return BadRequest(new ResponseDTO<object>(400, "Người dùng không có địa chỉ ví blockchain!", null));
                }
                ResponseDTO<object> result = await _bookingService.CreateBooking(bookingDTO, userId);
                if (result.StatusCode != 200)
                {
                    return StatusCode(result.StatusCode, result);
                }
                Booking booking = result.Data as Booking;
                booking.UserId = userId;
                await _bookingService.Add(booking);

                var allSlots = await _slotService.GetAll(x => x.ExpiredTime.Date > DateTime.Now.Date);
                var allAreasWithDetails = await _areaService.GetAllWithInclude(a => a.Room, a => a.AreaType, a => a.Positions);
                //var filteredAreas = allAreasWithDetails.Where(a => areasInRoom.Select(ar => ar.AreaId).Contains(a.AreaId)).ToList();
                var areaTypeIds = allAreasWithDetails.Select(a => a.AreaTypeId).Distinct().ToList();
                var areaTypes = await _areaTypeService.GetAll(at => areaTypeIds.Contains(at.AreaTypeId));

                // Tạo lookup để tra cứu nhanh
                var areaLookup = allAreasWithDetails.ToDictionary(a => a.AreaId, a => a);
                var areaTypeLookup = areaTypes.ToDictionary(at => at.AreaTypeId, at => at);

                var responseData = new
                {
                    BookingId = booking.BookingId,
                    BookingCreatedDate = booking.BookingCreatedDate,
                    TotalPrice = booking.Price,
                    Details = booking.BookingDetails.Select(bd =>
                    {
                        string positionDisplay = null;
                        string areaName = null;
                        string areaTypeName = null;
                        string roomName = null;

                        if (bd.AreaId.HasValue && areaLookup.TryGetValue(bd.AreaId.Value, out var areaFromAreaId))
                        {
                            areaName = areaFromAreaId.AreaName;
                            positionDisplay = areaTypeLookup.TryGetValue(areaFromAreaId.AreaTypeId, out var areaType) ? areaType.AreaTypeName : "N/A";
                            roomName = areaFromAreaId.Room?.RoomName;
                            areaTypeName = positionDisplay;
                        }
                        else if (bd.PositionId.HasValue)
                        {
                            var areaFromPosition = areaLookup.Values.FirstOrDefault(a => a.Positions.Any(p => p.PositionId == bd.PositionId));
                            if (areaFromPosition != null)
                            {
                                positionDisplay = areaFromPosition.Positions.FirstOrDefault(p => p.PositionId == bd.PositionId)?.PositionNumber.ToString() ?? "N/A";
                                areaName = areaFromPosition.AreaName;
                                areaTypeName = areaFromPosition.AreaTypeId != 0 && areaTypeLookup.TryGetValue(areaFromPosition.AreaTypeId, out var at) ? at.AreaTypeName : "N/A";
                                roomName = areaFromPosition.Room?.RoomName;
                            }
                            else
                            {
                                positionDisplay = "N/A";
                                areaName = "N/A";
                                areaTypeName = "N/A";
                                roomName = "N/A";
                            }
                        }

                        var slot = allSlots.FirstOrDefault(s => s.SlotId == bd.SlotId);
                        return new
                        {
                            BookingDetailId = bd.BookingDetailId,
                            Position = positionDisplay,
                            AreaName = areaName,
                            AreaTypeName = areaTypeName,
                            RoomName = roomName,
                            SlotNumber = slot?.SlotNumber
                        };
                    }).ToList()
                };
                return Ok(new ResponseDTO<object>(200, "Đặt phòng thành công!", responseData));

            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi đặt phòng", ex.Message));
            }

        }

        [HttpGet("availiblepos")]
        public async Task<IActionResult> GetAvailableSlot([FromQuery] AvailableSlotRequestDTO availableSlotRequestDTO)
        {
            ResponseDTO<object> result = await _bookingService.GetAvailableSlot(availableSlotRequestDTO);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("categoryinroom")]
        public async Task<IActionResult> GetAllAreaCategoryInRoom(int id)
        {
            ResponseDTO<object> result = await _roomService.GetAllAreaCategoryInRoom(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete]
        public async Task<IActionResult> Cancel(int bookingDetaitId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
            }
            var user = await _userService.Get(u => u.UserId == userId);
            if (user == null || string.IsNullOrEmpty(user.WalletAddress))
            {
                return BadRequest(new ResponseDTO<object>(400, "Người dùng không có địa chỉ ví blockchain!", null));
            }
            ResponseDTO<object> result = await _bookingService.Cancel(bookingDetaitId, userId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("bookingcancelinfo")]
        public async Task<IActionResult> GetCancelInfo(int bookingId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
            }
            var user = await _userService.Get(u => u.UserId == userId);
            if (user == null || string.IsNullOrEmpty(user.WalletAddress))
            {
                return BadRequest(new ResponseDTO<object>(400, "Người dùng không có địa chỉ ví blockchain!", null));
            }
            ResponseDTO<object> result = await _bookingService.GetCancelInfo(bookingId, userId);
            return StatusCode(result.StatusCode, result);
        }
    }

}