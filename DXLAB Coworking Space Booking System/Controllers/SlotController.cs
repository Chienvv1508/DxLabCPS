using AutoMapper;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Service.Sevices;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/[controller]")]
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
        [HttpPost("Create")]
        public async Task<IActionResult> CreateSlots([FromBody] SlotGenerationRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { Message = "Nội dung yêu cầu là bắt buộc!" });
                }

                if (!TimeSpan.TryParse(request.StartTime, out TimeSpan startTime) ||
                    !TimeSpan.TryParse(request.EndTime, out TimeSpan endTime))
                {
                    return BadRequest(new { Message = "Định dạng thời gian không hợp lệ!" });
                }

                if (startTime >= endTime)
                {
                    return BadRequest(new { Message = "StartTime phải sớm hơn EndTime!" });
                }

                int breakTime = request.BreakTime ?? 10;

                var slots = await _slotService.CreateSlots(startTime, endTime, breakTime);
                await _slotService.AddMany(slots);

                return Ok(new
                {
                    Message = $"{slots.Count} slots được tạo thành công!",
                    Slots = slots.Select(slot => _mapper.Map<SlotDTO>(slot))
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tạo slots: " + ex.Message });
            }
        }
        // API Lấy tất cả slot
        [HttpGet]
        public async Task<IActionResult> GetAllSlots()
        {
            try
            {
                var slots = await _slotService.GetAll();
                var slotDTOs = _mapper.Map<List<SlotDTO>>(slots);
                return Ok(slotDTOs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy slots: " + ex.Message });
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
                    return NotFound(new { Message = $"Slot với id: {id} không tìm thấy!" });
                }

                var slotDTO = _mapper.Map<SlotDTO>(slot);
                return Ok(slotDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi truy xuất slot: " + ex.Message });
            }
        }
    }
}
