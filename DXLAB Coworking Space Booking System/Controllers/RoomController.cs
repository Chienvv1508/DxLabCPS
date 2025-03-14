using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NBitcoin.Secp256k1;
using Nethereum.Contracts.Standards.ERC20.TokenList;
using Thirdweb;



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
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                var response = new ResponseDTO<object>(400, "Lỗi: " + string.Join("; ", errors), null);
                return BadRequest(response);
            }

            var existedRoom =  await _roomService.Get(x => x.RoomName == roomDto.RoomName);
           
            if(existedRoom != null)
            {
                var response = new ResponseDTO<object>(400, "Tên phòng đã tồn tại!", null);
                return BadRequest(response);
            }

            try
            {
                var room = _mapper.Map<Room>(roomDto);
                await _roomService.Add(room);
                var response = new ResponseDTO<RoomDTO>(201, "Tạo phòng thành công", roomDto);
                return CreatedAtAction(nameof(GetRoomById), new { id = room.RoomId }, response);
            }
            catch (DbUpdateException ex)  // Lỗi liên quan đến database
            {
                var response = new ResponseDTO<object>(500, "Lỗi cập nhật cơ sở dữ liệu.", ex.Message);
                return StatusCode(500, response);
            }
            catch (Exception ex)  // Các lỗi khác
            {
                var response = new ResponseDTO<object>(500, "Đã xảy ra lỗi khi tạo phòng.", ex.Message);
                return StatusCode(500, response);
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
