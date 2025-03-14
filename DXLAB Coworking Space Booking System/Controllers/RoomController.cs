using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NBitcoin.Secp256k1;
using Nethereum.Contracts.Standards.ERC20.TokenList;



namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly IMapper _mapper;

        public RoomController(IRoomService roomService, IMapper mapper)
        {
            _roomService = roomService;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] RoomDTO roomDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var room = _mapper.Map<Room>(roomDto);
                await _roomService.Add(room);

                return Ok(roomDto);
            }
            catch (DbUpdateException ex)  // Lỗi liên quan đến database
            {
                return StatusCode(500, new { message = "Lỗi cập nhật cơ sở dữ liệu.", details = ex.Message });
            }
            catch (Exception ex)  // Các lỗi khác
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo phòng.", details = ex.Message });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchRoom(int id, [FromBody] JsonPatchDocument<Room> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest(new { message = "Chưa có dữ liệu update" });
            }

            var result = await _roomService.PatchRoomAsync(id, patchDoc);
            if (!result)
            {
                return NotFound(new { message = "Cập nhập dữ liệu lỗi" });
            }

            return Ok(new { message = "Cập nhập dữ liệu thành công" });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomDTO>>> GetAllRooms()
        {
            var rooms = await _roomService.GetAll();
            var roomDtos = _mapper.Map<IEnumerable<RoomDTO>>(rooms);
            return Ok(roomDtos);
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomDTO>> GetRoomById(int id)
        {
            var room = await _roomService.Get(x => x.RoomId == id);
            if (room == null)
            {
                return NotFound(new { message = "Room not found" });
            }
            var roomDto = _mapper.Map<RoomDTO>(room);
            return Ok(roomDto);
        }
    }
}
