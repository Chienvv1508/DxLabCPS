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
        public async Task<IActionResult> CreateRoom([FromBody] RoomDTO roomDto)
        {

            if (roomDto.Area_DTO == null)
            {
                var response = new ResponseDTO<object>(400, "Bạn phải thêm khu vực cho phòng", null);
                return BadRequest(response);
            }
            if (roomDto.Area_DTO.Count() == 0)
            {
                var response = new ResponseDTO<object>(400, "Bạn phải thêm khu vực cho phòng", null);
                return BadRequest(response);
            }

            var existedRoom = await _roomService.Get(x => x.RoomName == roomDto.RoomName);

            if (existedRoom != null)
            {
                var response = new ResponseDTO<object>(400, "Tên phòng đã tồn tại!", null);
                return BadRequest(response);
            }

            try
            {

                //check size
                int areas_totalSize = 0;
                var areaTypeList = await _areaTypeService.GetAll();
                var areaTypeListPara = new List<AreaType>();
                foreach (var area in roomDto.Area_DTO)
                {

                    var areatype = areaTypeList.FirstOrDefault(x => x.AreaTypeId == area.AreaTypeId);
                    if (areatype != null)
                    {
                        areas_totalSize += areatype.Size;
                        areaTypeListPara.Add(areatype);

                    }
                    else
                    {
                        var response1 = new ResponseDTO<object>(400, $"Mã khu vực {area.AreaTypeId} không tồn tại!", null);
                        return BadRequest(response1);
                    }
                }

                if (areas_totalSize > roomDto.Capacity)
                {
                    var response1 = new ResponseDTO<object>(400, $"Sức chứa của phòng là: {roomDto.Capacity}. Nhưng tổng chỗ trong khu vực của bạn đã quá!", null);
                    return BadRequest(response1);
                }


                //check arename

                var areaList = roomDto.Area_DTO;
                var areaNameList = new List<string>();
                var araeExistedList = await _areaService.GetAll();
                foreach(var area in areaList)
                {
                    if(areaNameList.FirstOrDefault(x => x.Equals(area.AreaName)) != null)
                    {
                        var response1 = new ResponseDTO<object>(400, "Tên khu vực đang nhập trùng nhau", null);
                        return BadRequest(response1);
                    }
                    areaNameList.Add(area.AreaName);
                    //if(araeExistedList != null)
                    //{
                    //    if(araeExistedList.Count() != 0)
                    //    {
                    //        if(araeExistedList.FirstOrDefault(x => x.AreaName == area.AreaName) != null)
                    //        {
                    //            var response1 = new ResponseDTO<object>(400, $"Tên khu vực {area.AreaName} đã tồn tại", null);
                    //            return BadRequest(response1);
                    //        }
                    //    }
                    //}
                }


                var room = _mapper.Map<Room>(roomDto);
                //var individualAreaTypeList = areaTypeListPara.Where(x => x.AreaCategory == 2);
                //if (individualAreaTypeList != null)
                //{
                //    foreach (var areatype in individualAreaTypeList)
                //    {
                //        int[] position = Enumerable.Range(1, areatype.Size).ToArray();
                //        List<Position> positions = new List<Position>();
                //        for (int i = 0; i < position.Length; i++)
                //        {
                //            var areaPosition = new Position();
                //            areaPosition.Status = 0;
                //            areaPosition.PositionNumber = i;
                //            positions.Add(areaPosition);
                //        }
                //        var area = room.Areas.FirstOrDefault(x => x.AreaTypeId == areatype.AreaTypeId);
                //        area.Positions = positions;
                //    }
                //}
                await _roomService.Add(room);
                roomDto = _mapper.Map<RoomDTO>(room);

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
                var response = new ResponseDTO<object>(400, "Bạn chưa truyền dữ liệu vào", null);
                return BadRequest(response);
            }


            var roomNameOp = patchDoc.Operations.FirstOrDefault(op => op.path.Equals("roomName", StringComparison.OrdinalIgnoreCase));
            if (roomNameOp != null)
            {
                var existedRoom = await _roomService.Get(x => x.RoomName == roomNameOp.value.ToString());
                if (existedRoom != null)
                {
                    var response = new ResponseDTO<object>(400, $"Tên loại phòng {existedRoom} đã tồn tại. Vui lòng nhập tên loại phòng khác!", null);
                    return BadRequest(response);
                }
            }







            var roomFromDb = await _roomService.Get(r => r.RoomId == id);
            if (roomFromDb == null)
            {
                var response = new ResponseDTO<object>(404, $"Không tìm thấy phòng có id {id}!", null);
                return NotFound(response);
            }

            

            patchDoc.ApplyTo(roomFromDb, ModelState);  

            
            if (!ModelState.IsValid)
            {
                var allErrors = ModelState
                .SelectMany(ms => ms.Value.Errors
                .Select(err => $"{ms.Key}: {err.ErrorMessage}"))
                .ToList();
                string errorString = string.Join(" | ", allErrors);
                var response = new ResponseDTO<object>(400, errorString, null);
                return BadRequest(response);
            }

            var roomDTO = _mapper.Map<RoomDTO>(roomFromDb);
            
            bool isValid = TryValidateModel(roomDTO);
            if (!isValid)
            {
                var allErrors = ModelState
                .SelectMany(ms => ms.Value.Errors
                .Select(err => $"{ms.Key}: {err.ErrorMessage}"))
                .ToList();

                string errorString = string.Join(" | ", allErrors);
                var response = new ResponseDTO<object>(404, errorString, null);
                return BadRequest(response);  
            }
            await _roomService.Update(roomFromDb);
            var response2 = new ResponseDTO<object>(200, $"Cập nhập thành công phòng {id}!", null);
            return Ok(response2);

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomDTO>>> GetAllRooms()
        {
            var rooms = await _roomService.GetAll();
            var roomDtos = _mapper.Map<IEnumerable<RoomDTO>>(rooms);
            var response = new ResponseDTO<object>(200, "Lấy thành công", roomDtos);
            return Ok(response);
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomDTO>> GetRoomById(int id)
        {
            var room = await _roomService.Get(x => x.RoomId == id);
            if (room == null)
            {
                var responseNotFound = new ResponseDTO<object>(404, "Mã Room không tồn tại", null);
                return NotFound(responseNotFound);
            }
            var roomDto = _mapper.Map<RoomDTO>(room);
            var response = new ResponseDTO<object>(200, "Lấy thành công", roomDto);
            return Ok(response);
        }
    }
}
