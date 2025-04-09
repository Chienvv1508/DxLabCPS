using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NBitcoin.Secp256k1;
using Nethereum.Contracts.Standards.ERC20.TokenList;
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
        public async Task<IActionResult> CreateRoom([FromBody] RoomDTO roomDto)
        {
            if (roomDto.Area_DTO == null || !roomDto.Area_DTO.Any())
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
                // Check size
                int areas_totalSize = 0;
                var areaTypeList = await _areaTypeService.GetAll();
                Area individualArea = null;
                int inputIndividual = 0;

                foreach (var area in roomDto.Area_DTO)
                {
                    var areatype = areaTypeList.FirstOrDefault(x => x.AreaTypeId == area.AreaTypeId);
                    if (areatype != null)
                    {
                        areas_totalSize += areatype.Size;
                        if (areatype.AreaCategory == 1)
                        {
                            inputIndividual++;
                            if (inputIndividual > 1)
                            {
                                var response1 = new ResponseDTO<object>(400, "Trong phòng chỉ được có 1 phòng cá nhân", null);
                                return BadRequest(response1);
                            }
                            individualArea = _mapper.Map<Area>(area);
                            individualArea.AreaType = areatype;
                        }
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

                // Check area name duplicates
                var areaNameList = new List<string>();
                foreach (var area in roomDto.Area_DTO)
                {
                    if (areaNameList.Contains(area.AreaName))
                    {
                        var response1 = new ResponseDTO<object>(400, "Tên khu vực đang nhập trùng nhau", null);
                        return BadRequest(response1);
                    }
                    areaNameList.Add(area.AreaName);
                }

                // Ánh xạ từ RoomDTO sang Room
                var room = _mapper.Map<Room>(roomDto);

                // Thêm position cho khu vực cá nhân
                if (individualArea != null)
                {
                    var xr = room.Areas.FirstOrDefault(x => x.AreaTypeId == individualArea.AreaTypeId);
                    if (xr != null)
                    {
                        int[] position = Enumerable.Range(1, individualArea.AreaType.Size).ToArray();
                        List<Position> positions = new List<Position>();
                        for (int i = 1; i <= position.Length; i++)
                        {
                            var pos = new Position
                            {
                                PositionNumber = i,
                                Status = true
                            };
                            positions.Add(pos);
                        }
                        xr.Positions = positions;
                    }
                }

                // Lưu room vào database
                await _roomService.Add(room);

                // Tải lại room từ database với Areas, Images và AreaType
                var savedRoom = await _roomService.GetWithInclude(
                    r => r.RoomId == room.RoomId,
                    r => r.Areas // Include Areas
                );

                if (savedRoom != null)
                {
                    foreach (var area in savedRoom.Areas)
                    {
                        var areaWithImages = await _areaService.GetWithInclude(
                            a => a.AreaId == area.AreaId,
                            a => a.Images
                        );
                        area.Images = areaWithImages.Images.ToList();
                        area.AreaType = await _areaTypeService.Get(at => at.AreaTypeId == area.AreaTypeId);
                    }
                }

                // Ánh xạ sang RoomDTO để trả về
                roomDto = _mapper.Map<RoomDTO>(savedRoom);

                var response = new ResponseDTO<RoomDTO>(201, "Tạo phòng thành công", roomDto);
                return CreatedAtAction(nameof(GetRoomById), new { id = room.RoomId }, response);
            }
            catch (DbUpdateException ex)
            {
                var response = new ResponseDTO<object>(500, "Lỗi cập nhật cơ sở dữ liệu.", ex.Message);
                return StatusCode(500, response);
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(500, "Đã xảy ra lỗi khi tạo phòng.", ex.Message);
                return StatusCode(500, response);
            }
        }

        [HttpPatch("{id}")]
        //[Authorize(Roles = "Admin")]
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


            var allowedPaths = new HashSet<string>
             {
            "areaTypeName",
             "areaDescription",
             "price"


            };
            var allowedPaths = new HashSet<string>
            {
            "areaTypeName",
             "areaDescription",
             "price"


            };


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
            // Tải tất cả Room với Areas, Images của Areas và AreaType
            var rooms = await _roomService.GetAll();

            foreach (var r in rooms)
            {
                foreach (var a in r.Areas)
                {
                    // Tải Images và AreaType cho mỗi Area
                    var areaWithDetails = await _areaService.GetWithInclude(
                        x => x.AreaId == a.AreaId,
                        x => x.Images,
                        x => x.AreaType
                    );
                    a.Images = areaWithDetails.Images.ToList();
                    a.AreaType = areaWithDetails.AreaType;
                }
            }
            var roomDtos = _mapper.Map<IEnumerable<RoomDTO>>(rooms);
            var response = new ResponseDTO<object>(200, "Lấy thành công", roomDtos);
            return Ok(response);
        }
       
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomDTO>> GetRoomById(int id)
        {
            // Tải Room với Areas và Images của Room
            var room = await _roomService.Get(x => x.RoomId == id);
            if (room == null)
            {
                var responseNotFound = new ResponseDTO<object>(404, "Mã Room không tồn tại", null);
                return NotFound(responseNotFound);
            }

            foreach (var a in room.Areas)
            {
                // Tải Images và AreaType cho mỗi Area
                var areaWithDetails = await _areaService.GetWithInclude(
                    x => x.AreaId == a.AreaId,
                    x => x.Images,
                    x => x.AreaType
                );
                a.Images = areaWithDetails.Images.ToList();
                a.AreaType = areaWithDetails.AreaType;
            }

            var roomDto = _mapper.Map<RoomDTO>(room);
            var response = new ResponseDTO<object>(200, "Lấy thành công", roomDto);
            return Ok(response);
        }

        [HttpGet("area")]
        public async Task<IActionResult> GetAllAreas()
        {
            // Tải tất cả Areas với AreaType, Positions và Images
            var areas = await _areaService.GetAllWithInclude(
                x => x.AreaType,
                x => x.Positions,
                x => x.Images // Thêm Images
            );

            if (!areas.Any())
            {
                return NotFound(new ResponseDTO<object>(404, "Không tìm thấy khu vực nào!", null));
            }

            var areaDtos = _mapper.Map<IEnumerable<AreaDTO>>(areas);

            var responseData = areas.Select(a => new
            {
                AreaId = a.AreaId,
                AreaName = a.AreaName,
                AreaDescription = a.AreaDescription,
                AreaImage = a.Images != null ? a.Images.Select(i => i.ImageUrl).ToList() : null // Sửa để trả về danh sách URL
            }).ToList();

            var response = new ResponseDTO<object>(200, "Lấy tất cả khu vực thành công", responseData);
            return Ok(response);
        }

        [HttpGet("area/{id}")]
        public async Task<IActionResult> GetAreaById(int id)
        {
            // Tải Area với Images và AreaType
            var area = await _areaService.GetWithInclude(
                a => a.AreaId == id,
                a => a.Images,
                a => a.AreaType
            );

            if (area == null)
            {
                return NotFound(new ResponseDTO<object>(404, $"Không tìm thấy khu vực có id {id}!", null));
            }

            var responseData = new
            {
                AreaId = area.AreaId,
                AreaName = area.AreaName,
                AreaDescription = area.AreaDescription,
                AreaImage = area.Images != null ? area.Images.Select(i => i.ImageUrl).ToList() : null // Sửa để trả về danh sách URL
            };

            var response = new ResponseDTO<object>(200, "Lấy khu vực thành công", responseData);
            return Ok(response);
        }

        [HttpPost("newImage")]
        public async Task<IActionResult> AddNewImage(int id, [FromForm] List<IFormFile> Images)
        {

            try
            {
                var roomFromDb = await _roomService.Get(x => x.RoomId == id);
                if (roomFromDb == null)
                    return BadRequest(new ResponseDTO<object>(400, "Không tìm thấy phòng này!", null));
                if (Images == null)
                    return BadRequest(new ResponseDTO<object>(400, "Bắt buộc nhập ảnh", null));
                var result = await ImageSerive.AddImage(Images);
                if (result.Item1 == true)
                {
                    foreach (var i in result.Item2)
                    {
                        roomFromDb.Images.Add(new Image() { ImageUrl = i });

                    }
                }
                else
                    return BadRequest(new ResponseDTO<object>(400, "Cập nhập lỗi!", null));

                await _roomService.Update(roomFromDb);
                return Ok(new ResponseDTO<object>(200, "Cập nhập thành công", null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, "Cập nhập lỗi!", null));
            }
        }


        [HttpDelete("Images")]
        public async Task<IActionResult> RemoveImage(int id, [FromBody] List<string> images)
        {
            try
            {
                var roomFromDb = await _roomService.GetWithInclude(x => x.RoomId == id, x => x.Images);
                if (roomFromDb == null)
                    return BadRequest(new ResponseDTO<object>(400, "Không tìm thấy phòng này!", null));
                if (images == null)
                    return BadRequest(new ResponseDTO<object>(400, "Bắt buộc nhập ảnh", null));
                var imageList = roomFromDb.Images;
                if (imageList.Count <= images.Count)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Không được xóa hết ảnh", null));
                }

                foreach (var image in images)
                {
                    var item = imageList.FirstOrDefault(x => x.ImageUrl == $"{image}");
                    if (item == null)
                        return BadRequest(new ResponseDTO<object>(400, "Ảnh không tồn tại trong phòng!", null));
                    roomFromDb.Images.Remove(item);

                }
                await _roomService.UpdateImage(roomFromDb, images);


                foreach (var image in images)
                {
                    ImageSerive.RemoveImage(image);
                }
                return Ok(new ResponseDTO<object>(200, "Cập nhập thành công!", null));

            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, "Cập nhập lỗi!", null));
            }
        }
    }
}
