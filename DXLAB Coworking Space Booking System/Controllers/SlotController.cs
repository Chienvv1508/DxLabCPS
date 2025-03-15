using AutoMapper;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Service.Sevices;
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
        public async Task<IActionResult> CreateSlots([FromBody] SlotGenerationRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ResponseDTO<object>("Nội dung yêu cầu là bắt buộc!", null));
            }

            if (!TimeSpan.TryParse(request.StartTime, out TimeSpan startTime) ||
                !TimeSpan.TryParse(request.EndTime, out TimeSpan endTime))
            {
                return BadRequest(new ResponseDTO<object>("Định dạng thời gian không hợp lệ!", null));
                ;
            }

            if (startTime >= endTime)
            {
                return BadRequest(new ResponseDTO<object>("StartTime phải sớm hơn EndTime!", null));
            }

            int breakTime = request.BreakTime ?? 10;
            try
            {
                var slots = await _slotService.CreateSlots(startTime, endTime, breakTime);
                await _slotService.AddMany(slots);

                var slotDtos = _mapper.Map<IEnumerable<SlotDTO>>(slots);
                return Ok(new ResponseDTO<IEnumerable<SlotDTO>>($"{slots.Count} slots được tạo thành công!", slotDtos));
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
                var slotDtos = _mapper.Map<IEnumerable<SlotDTO>>(slots);
                return Ok(new ResponseDTO<IEnumerable<SlotDTO>>("Lấy danh sách slot thành công.", slotDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>("Lỗi khi lấy danh sách slot: " + ex.Message, null));
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
                    return NotFound(new ResponseDTO<object>($"Slot với ID {id} không tìm thấy!", null));
                }

                var slotDto = _mapper.Map<SlotDTO>(slot);
                return Ok(new ResponseDTO<SlotDTO>("Lấy thông tin slot thành công.", slotDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>("Lỗi khi truy xuất slot: " + ex.Message, null));
            }
        }
    }
}
