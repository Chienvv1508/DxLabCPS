using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/slot")]
    [ApiController]
    public class SlotController : ControllerBase
    {
        private readonly ISlotService _slotService;
        private readonly IMapper _mapper;

        public SlotController(ISlotService slotService, IMapper mapper)
        {
            _slotService = slotService;
            _mapper = mapper;
        }
        // API Generate slot
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSlots([FromBody] SlotGenerationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<object>(400, "Dữ liệu đầu vào không hợp lệ!", ModelState));
            }
            TimeSpan startTime = TimeSpan.Parse(request.StartTime); // Hỗ trợ HH:mm:ss
            TimeSpan endTime = TimeSpan.Parse(request.EndTime);

            if (startTime >= endTime)
            {
                return BadRequest(new ResponseDTO<object>(400, "StartTime phải sớm hơn EndTime!", null));
            }
            int timeSLot = request.TimeSlot ?? throw new ArgumentException(nameof(timeSLot), "Time Slot là bắt buộc");
            int breakTime = request.BreakTime ?? throw new ArgumentException(nameof(breakTime), "Break Time là bắt buộc");
            try
            {
                var slots = await _slotService.CreateSlots(startTime, endTime, timeSLot, breakTime);
                await _slotService.AddMany(slots);

                var slotDtos = _mapper.Map<IEnumerable<SlotDTO>>(slots);
                return Ok(new ResponseDTO<IEnumerable<SlotDTO>>(200, $"{slots.Count} slots được tạo thành công!", slotDtos));
            }

            catch (InvalidOperationException ex)
            {
                return Conflict(new ResponseDTO<object>(409, ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi tạo slot: " + ex.Message, null));
            }
        }
        // API Lấy tất cả slot
        [HttpGet]
        public async Task<IActionResult> GetAllSlots()
        {
            try
            {
                var slots = await _slotService.GetAll();
                var slotDtos = _mapper.Map<IEnumerable<SlotDTO>>(slots);
                return Ok(new ResponseDTO<IEnumerable<SlotDTO>>(200, "Lấy danh sách slot thành công.", slotDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi lấy danh sách slot: " + ex.Message, null));
            }
        }

        // API Lấy slot theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSlotById(int id)
        {
            try
            {
                var slot = await _slotService.GetById(id);
                if (slot == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Slot với ID {id} không tìm thấy!", null));
                }

                var slotDto = _mapper.Map<SlotDTO>(slot);
                return Ok(new ResponseDTO<SlotDTO>(200, "Lấy thông tin slot thành công.", slotDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi truy xuất slot: " + ex.Message, null));
            }
        }
    }
}
