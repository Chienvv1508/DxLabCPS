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
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSlots([FromBody] SlotGenerationRequest request)
        {
            //if (!ModelState.IsValid)
            //{
            //    // Lấy lỗi từ ModelState
            //    var errors = ModelState
            //        .Where(m => m.Value.Errors.Any())
            //        .SelectMany(m => m.Value.Errors)
            //        .Select(e => e.ErrorMessage)
            //        .Where(e => !string.IsNullOrEmpty(e))
            //        .ToList();

            //    string errorMessage = errors.Any() ? string.Join(", ", errors) : "Dữ liệu đầu vào không hợp lệ!";
            //    return BadRequest(new ResponseDTO<object>(400, errorMessage, null));
            //}
            try
            {
                // Parse StartTime và EndTime
                if (!TimeSpan.TryParse(request.StartTime, out TimeSpan startTime))
                {
                    return BadRequest(new ResponseDTO<object>(400, "StartTime sai định dạng thời gian!", null));
                }
                if (!TimeSpan.TryParse(request.EndTime, out TimeSpan endTime))
                {
                    return BadRequest(new ResponseDTO<object>(400, "EndTime sai định dạng thời gian!", null));
                }

                if (startTime >= endTime)
                {
                    return BadRequest(new ResponseDTO<object>(400, "StartTime phải sớm hơn EndTime!", null));
                }

                int timeSlot = request.TimeSlot ?? throw new ArgumentException(nameof(timeSlot), "Time Slot là bắt buộc!");
                int breakTime = request.BreakTime ?? throw new ArgumentException(nameof(breakTime), "Break Time là bắt buộc!");

                var slots = await _slotService.CreateSlots(startTime, endTime, timeSlot, breakTime);
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
                var slots = await _slotService.GetAll(x => x.ExpiredTime.Date > DateTime.Now.Date);
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
                var slot = await _slotService.Get(x => x.SlotId == id && x.ExpiredTime.Date > DateTime.Now.Date);
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

        [HttpPut("{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSlot(int id)
        {
            try
            {
                // Kiểm tra slot có tồn tại không
                var existingSlot =  await _slotService.Get(x => x.SlotId == id && x.ExpiredTime.Date > DateTime.Now.Date);
                if (existingSlot == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Slot với ID {id} không tìm thấy!", null));
                }


                DateTime expiredDate = await _slotService.GetNewExpiredDate(id);
                existingSlot.ExpiredTime = expiredDate;

                //// Đổi trạng thái: 1 -> 0, 0 -> 1
                //int newStatus = existingSlot.Status == 1 ? 0 : 1;

                //// Tạo entity để cập nhật
                //var slotToUpdate = new Slot
                //{
                //    SlotId = id,
                //    Status = newStatus
                //};

                // Gọi service để cập nhật
                await _slotService.Update(existingSlot);

                // Map lại thành DTO để trả về
                var updatedSlotDto = _mapper.Map<SlotDTO>(existingSlot);
               /* updatedSlotDto.Status = newStatus; */// Đảm bảo DTO phản ánh Status mới
                return Ok(new ResponseDTO<SlotDTO>(200, $"Cập nhật trạng thái slot thành công!", updatedSlotDto));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ResponseDTO<object>(409, ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi cập nhật slot: " + ex.Message, null));
            }
        }
    }
}
