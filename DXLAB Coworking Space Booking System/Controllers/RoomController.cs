using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NBitcoin.Secp256k1;
using Nethereum.Contracts.Standards.ERC20.TokenList;
using Newtonsoft.Json;
using Thirdweb;



namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/room")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly IMapper _mapper;
        private readonly IAreaTypeService _areaTypeService;
        private readonly IAreaService _areaService;

        public RoomController(IRoomService roomService, IMapper mapper, IAreaTypeService areaTypeService, IAreaService areaService)
        {

            _roomService = roomService;
            _mapper = mapper;
            _areaTypeService = areaTypeService;
            _areaService = areaService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRoom([FromForm] RoomForAddDTO roomDto, [FromForm] string AreaAddDTO)
        {
            if (string.IsNullOrEmpty(AreaAddDTO))
            {
                return BadRequest(new ResponseDTO<object>(400, "Bạn phải thêm khu vực cho phòng", null));
            }

            try
            {
                var inputArea = JsonConvert.DeserializeObject<List<AreaAdd>>(AreaAddDTO);
                List<AreaDTO> listAreaDTO = new List<AreaDTO>();
                foreach (var item in inputArea)
                {
                    var area = new AreaDTO() { AreaTypeId = item.AreaTypeId, AreaName = item.AreaName, Status = 0 };
                    listAreaDTO.Add(area);
                }
                roomDto.Area_DTO = listAreaDTO;
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, "Lỗi truyền tham số phòng", null));
            }

            ResponseDTO<Room> result = await _roomService.AddRoom(roomDto);
            if (result.StatusCode == 200)
            {
                var room = result.Data;
                // Tải lại room từ database với Areas, Images và AreaType
                var savedRoom = await _roomService.GetWithInclude(
                    r => r.RoomId == room.RoomId,
                    r => r.Areas
                );

                if (savedRoom != null)
                {
                    foreach (var area in savedRoom.Areas)
                    {
                        area.AreaType = await _areaTypeService.Get(at => at.AreaTypeId == area.AreaTypeId);
                    }
                }

                // Ánh xạ sang RoomDTO để trả về
                var roomDtoRs = _mapper.Map<RoomDTO>(savedRoom);

                var response = new ResponseDTO<RoomDTO>(201, "Tạo phòng thành công", roomDtoRs);
                return CreatedAtAction(nameof(GetRoomById), new { id = room.RoomId }, response);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PatchRoom(int id, [FromBody] JsonPatchDocument<Room> patchDoc)
        {

            ResponseDTO<Room> result = await _roomService.PatchRoom(id, patchDoc);
            return StatusCode(result.StatusCode, result);
        }

        // Inactive room
        [HttpPut("room")]
        public async Task<IActionResult> RemoveRoom(int roomId)
        {

            ResponseDTO<Room> result = await _roomService.InactiveRoom(roomId);
            return StatusCode(result.StatusCode, result);
        }

        // admin quản lý 
        // student xem chỗ đặt phòng
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomDTO>>> GetAllRooms()
        {

            ResponseDTO<IEnumerable<Room>> result1 = await _roomService.GetAllRoomIncludeAreaAndAreaType();
            if(result1.StatusCode == 200)
            {
                var rooms = result1.Data;
                var roomDtos = _mapper.Map<IEnumerable<RoomDTO>>(rooms);
                var response = new ResponseDTO<object>(200, "Lấy thành công", roomDtos);
                return Ok(response);

            }
            else
                return StatusCode(result1.StatusCode, result1);
        }

        //Cho student xem
        //Cho cả admin xem chỗ quản lý room
        //Cho cả student xem chỗ matrix
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomDTO>> GetRoomById(int id)
        {

            // Tải Room với Areas và Images của Room
            var room = await _roomService.Get(x => x.RoomId == id && x.Status != 2);
            if (room.Areas != null)
            {
                if (room.Areas.Any())
                {
                    var areas = room.Areas.Where(x => x.Status != 2);
                    room.Areas = areas.ToList();
                }
            }

            foreach (var area in room.Areas)
            {
                area.AreaType = await _areaTypeService.GetWithInclude(x => x.AreaTypeId == area.AreaTypeId, x => x.AreaTypeCategory);

            }

            if (room == null)
            {
                var responseNotFound = new ResponseDTO<object>(404, "Mã Room không tồn tại", null);
                return NotFound(responseNotFound);
            }



            var roomDto = _mapper.Map<RoomDTO>(room);
            var response = new ResponseDTO<object>(200, "Lấy thành công", roomDto);
            return Ok(response);
        }

        [HttpPost("newImage")]
        public async Task<IActionResult> AddNewImage(int id, [FromForm] List<IFormFile> images)
        {
            ResponseDTO<Room> result = await _roomService.AddImages(id, images);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("Images")]
        public async Task<IActionResult> RemoveImage(int id, [FromBody] List<string> images)
        {

            ResponseDTO<Room> result = await _roomService.RemoveImages(id, images);
            return StatusCode(result.StatusCode, result);
        }
    }
}
