using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/area")]
    [ApiController]
    public class AreaController : ControllerBase
    {
        private readonly IAreaService _areaService;
        private readonly IUsingFacilytyService _usingFaclytyService;
        private readonly IFaciStatusService _facilityStatusService;
        private readonly IMapper _mapper;
        private readonly IFacilityService _facilityService;
        private readonly IRoomService _roomService;
        private readonly IAreaTypeService _areaTypeService;
        private readonly IAreaTypeCategoryService _areaTypeCategoryService;

        public AreaController(IAreaService areaService, IUsingFacilytyService usingFacilytyService,
            IFaciStatusService faciStatusService, IMapper mapper, IFacilityService facilityService,
            IRoomService roomService, IAreaTypeService areaTypeService, IAreaTypeCategoryService areaTypeCategoryService)
        {
            _usingFaclytyService = usingFacilytyService;
            _facilityStatusService = faciStatusService;
            _areaService = areaService;
            _mapper = mapper;
            _facilityService = facilityService;
            _roomService = roomService;
            _areaTypeService = areaTypeService;
            _areaTypeCategoryService = areaTypeCategoryService;
        }

        [HttpPost("faci")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddFaciToArea(int areaid, int status, FaciAddDTO faciAddDTO)
        {
            try
            {

                var areaInRoom = await _areaService.GetWithInclude(x => x.AreaId == areaid, x => x.AreaType);
                if (areaInRoom == null)
                {
                    var reponse = new ResponseDTO<object>(404, "Không tìm thấy khu vực. Vui lòng nhập lại khu vực", null);
                    return NotFound(reponse);
                }
                //0-mơi 1-dasudung 2--hong
                if (status < 0 && status > 1)
                {
                    var reponse = new ResponseDTO<object>(400, "Thiết bị được nhập phải mới hoặc vẫn sử dụng được", null);
                    return BadRequest(reponse);
                }
                var usingFacilities = await _usingFaclytyService.GetAllWithInclude(x => x.AreaId == areaid, x => x.Facility);
                int numberOfPositionT = 0;
                int numberOfPositionCh = 0;  // thay doi
                foreach (var faci in usingFacilities)
                {
                    if (faci.Facility.FacilityCategory == 1)
                    {
                        numberOfPositionT += faci.Facility.Size * faci.Quantity; // sửa
                    }
                    else
                        numberOfPositionCh += faci.Quantity;// sửa

                }
                bool isFullT = false;
                bool isFullCh = false;
                var fullInfoOfFaci = await _facilityService.Get(x => x.FacilityId == faciAddDTO.FacilityId && x.BatchNumber == faciAddDTO.BatchNumber
                && x.ImportDate == faciAddDTO.ImportDate);
                if (fullInfoOfFaci == null)
                {
                    var reponse = new ResponseDTO<object>(400, "Thông tin thiết bị nhập sai. Vui lòng nhập lại", null);
                    return BadRequest(reponse);
                }
                if (fullInfoOfFaci.FacilityCategory == 1)
                {
                    if (areaInRoom.AreaType.Size - numberOfPositionT < faciAddDTO.Quantity * fullInfoOfFaci.Size)
                    {
                        var reponse = new ResponseDTO<object>(400, "Bạn đã nhập quá số lượng bàn cho phép của phòng", null); // Sửa
                        return BadRequest(reponse);
                    }
                    if (areaInRoom.AreaType.Size - numberOfPositionT == faciAddDTO.Quantity * fullInfoOfFaci.Size)
                        isFullT = true;
                    if (areaInRoom.AreaType.Size  == numberOfPositionCh)
                        isFullCh = true;
                }
                // thêm đoạn này
                if (fullInfoOfFaci.FacilityCategory == 0)
                {
                    if (areaInRoom.AreaType.Size - numberOfPositionCh < faciAddDTO.Quantity)
                    {
                        var reponse = new ResponseDTO<object>(400, "Bạn đã nhập quá số lượng ghế cho phép của phòng", null); // Sửa
                        return BadRequest(reponse);
                    }
                    if (areaInRoom.AreaType.Size - numberOfPositionCh == faciAddDTO.Quantity)
                        isFullCh = true;
                    if (areaInRoom.AreaType.Size  == numberOfPositionT)
                        isFullT = true;
                }


                //check lượng đang có trong status

                var faciInStatus = await _facilityStatusService.Get(x => x.FacilityId == faciAddDTO.FacilityId && x.BatchNumber == faciAddDTO.BatchNumber
                && x.ImportDate == faciAddDTO.ImportDate && x.Status == status);

                if (faciInStatus == null)
                {
                    string tt = status == 0 ? "Mới" : "Đã sử dụng";
                    var reponse = new ResponseDTO<object>(400, $"Với trạng thái {tt} hiện không có thiết bị này", null);
                    return BadRequest(reponse);
                }
                if (faciInStatus.Quantity < faciAddDTO.Quantity)
                {
                    string tt = status == 0 ? "Mới" : "Đã sử dụng";
                    var reponse = new ResponseDTO<object>(400, $"Với trạng thái {tt} hiện không có đủ {faciAddDTO.Quantity} thiết bị", null);
                    return BadRequest(reponse);
                }


                //Thêm đoạn check xem trong phòng có using này chưa. Nếu có thì cộng nếu không thì thêm mới

                var existedFaciInArea = await _usingFaclytyService.Get(x => x.AreaId == areaid && x.BatchNumber == faciAddDTO.BatchNumber

                && x.FacilityId == faciAddDTO.FacilityId && x.ImportDate == faciAddDTO.ImportDate);
                bool statusOfArea = isFullCh && isFullT;
                if (existedFaciInArea == null)
                {
                    var newUsingFacility = new UsingFacility
                    {
                        AreaId = areaid,
                        BatchNumber = faciAddDTO.BatchNumber,
                        FacilityId = faciAddDTO.FacilityId,

                        Quantity = faciAddDTO.Quantity,
                        ImportDate = faciAddDTO.ImportDate
                    };

                    await _usingFaclytyService.Add(newUsingFacility, status, statusOfArea);
                }
                else
                {
                    existedFaciInArea.Quantity += faciAddDTO.Quantity;
                    await _usingFaclytyService.Update(existedFaciInArea, status, statusOfArea);
                }







                //var responseData = new
                //{
                //    AreaName = newUsingFacility.Area.AreaName,
                //    FacilityId = newUsingFacility.FacilityId,
                //    BatchNumber = newUsingFacility.BatchNumber,
                //    ImportDate = newUsingFacility.ImportDate,
                //    Quantity = newUsingFacility.Quantity
                //};


                return Ok(new ResponseDTO<object>(200, "Thêm thiết bị thành công", null));
            }
            catch (Exception ex)
            {
                var reponse = new ResponseDTO<object>(400, "Thêm thiết bị lỗi", null);
                return BadRequest(reponse);
            }

        }
        [HttpGet("allfacistatus")]
        public async Task<IActionResult> GetAllFaciStatus()
        { // Mục đích cho admin chon faci thêm vào phòng
            try
            {
                var faciStatusList = await _facilityStatusService.GetAllWithInclude(x => ((x.Status == 0 || x.Status == 1) && x.Quantity >= 0), x => x.Facility);
                var faciDTOs = _mapper.Map<List<FaciStatusDTO>>(faciStatusList);
                var response = new ResponseDTO<object>(200, "Lấy thành công", faciDTOs);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(500, ex.Message + ex.StackTrace, null);
                return StatusCode(500, response);

            }

        }

        [HttpGet("allusingfaci")]
        public async Task<IActionResult> GetAllUsingFaci()
        {
            try
            {
                var usingFacilities = await _usingFaclytyService.GetAllWithInclude(
                    x => true,
                    x => x.Area,
                    x => x.Facility
                );

                if (!usingFacilities.Any())
                {
                    return Ok(new ResponseDTO<object>(200, "Không có dữ liệu thiết bị đang sử dụng", new List<UsingFacilityDTO>()));
                }

                var usingFacilityDTOs = _mapper.Map<List<UsingFacilityDTO>>(usingFacilities);

                return Ok(new ResponseDTO<object>(200, "Lấy thành công danh sách thiết bị đang sử dụng", usingFacilityDTOs));
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(500, "Lỗi cơ sở dữ liệu!", null);
                return StatusCode(500, response);
            }
        }


        [HttpPost("faciremoving")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveFaciFromArea([FromBody] RemovedFaciDTO removedFaciDTO)
        {
            try
            {
                var existedFaciInArea = await _usingFaclytyService.Get(x => x.FacilityId == removedFaciDTO.FacilityId &&
                 x.AreaId == removedFaciDTO.AreaId
                 );
                if (existedFaciInArea == null)
                {
                    var response = new ResponseDTO<object>(400, "Không thấy thiết bị hoặc phòng tương ứng. Vui lòng nhập lại", null);
                    return BadRequest(response);
                }

                await _usingFaclytyService.Update(removedFaciDTO);

                return Ok(new ResponseDTO<object>(200, "Cập nhập thành công", null));

            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi cơ sở dữ liệu", null));
            }

        }

        [HttpGet("areainroom")]
        public async Task<IActionResult> GetAreasInRoom(int roomId)
        {
            var room = await _roomService.Get(x => x.RoomId == roomId);
            if (room == null)
            {
                var response = new ResponseDTO<object>(400, "Bạn nhập phòng không tồn tại", null);
                return BadRequest(response);

            }
            List<AreaGetDTO> areaDTOs = _mapper.Map<List<AreaGetDTO>>(room.Areas);
            var response1 = new ResponseDTO<object>(200, "Danh sách phòng", areaDTOs);
            return Ok(response1);
        }

        [HttpGet("faciinare")]
        public async Task<IActionResult> GetAllFaciInArea(int areaid)
        {
            try
            {
                var usingFacilities = await _usingFaclytyService.GetAllWithInclude(
                    x => x.AreaId == areaid,
                    x => x.Area,
                    x => x.Facility
                );

                if (!usingFacilities.Any())
                {
                    return Ok(new ResponseDTO<object>(200, "Không có dữ liệu thiết bị đang sử dụng", new List<UsingFacilityDTO>()));
                }

                var usingFacilityDTOs = _mapper.Map<List<UsingFacilityDTO>>(usingFacilities);

                return Ok(new ResponseDTO<object>(200, "Lấy thành công danh sách thiết bị đang sử dụng", usingFacilityDTOs));
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(500, "Lỗi cơ sở dữ liệu", null);
                return StatusCode(500, response);
            }
        }

        [HttpPost("newarea")]
        public async Task<IActionResult> AddNewAreaToRoom(int roomId, [FromBody] List<AreaAdd> areaAdds)
        {
            try
            {
                try
                {
                    var room = await _roomService.Get(x => x.RoomId == roomId);
                    if (room == null)
                    {
                        return BadRequest(new ResponseDTO<object>(400, "Lỗi phòng không tồn tại!", null));
                    }

                    //Check có thêm được area nữa không
                    // Check size
                    int areas_totalSize = 0;
                    var areaTypeList = await _areaTypeService.GetAll();
                    Area individualArea = null;
                    int countIndividualAre = 0;


                    foreach (var area in room.Areas)
                    {
                        var areatype = areaTypeList.FirstOrDefault(x => x.AreaTypeId == area.AreaTypeId);
                        if (areatype != null)
                        {
                            areas_totalSize += areatype.Size;
                            if (areatype.AreaCategory == 1)
                            {
                                countIndividualAre++;
                                individualArea = area;
                                individualArea.AreaType = areatype;
                            }
                        }
                        else
                        {
                            var response1 = new ResponseDTO<object>(400, $"Mã khu vực {area.AreaTypeId} không tồn tại!", null);
                            return BadRequest(response1);
                        }
                    }

                    if (areas_totalSize == room.Capacity)
                    {
                        var response1 = new ResponseDTO<object>(400, "Phòng đã đầy sức chứa. Không thể thêm khu vực", null);
                        return BadRequest(response1);
                    }



                    //Check areaTypeid có phù hợp ko

                    List<AreaType> areaTypesInput = new List<AreaType>();
                    foreach (var area in areaAdds)
                    {
                        var newArea = areaTypeList.FirstOrDefault(x => x.AreaTypeId == area.AreaTypeId);
                        if (newArea == null)
                        {
                            var response1 = new ResponseDTO<object>(400, "Nhập sai id của loại khu vực!", null);
                            return BadRequest(response1);
                        }
                        areaTypesInput.Add(newArea);
                    }

                    if (areaTypesInput.FirstOrDefault(x => x.AreaCategory == 1) != null && individualArea != null)
                    {
                        var response1 = new ResponseDTO<object>(400, "Trong phòng đã có loại cá nhân không thêm được loại cá nhân nữa!", null);
                        return BadRequest(response1);

                    }

                    if (countIndividualAre > 1)
                    {
                        var response1 = new ResponseDTO<object>(400, "Bạn đã nhập nhiều loại cá nhân! Chỉ được nhập 1 loại cá nhân trong phòng", null);
                        return BadRequest(response1);
                    }

                    // Check size


                    foreach (var areaType in areaTypesInput)
                    {
                        areas_totalSize += areaType.Size;
                    }

                    if (areas_totalSize > room.Capacity)
                    {
                        var response1 = new ResponseDTO<object>(400, "Bạn đã nhập quá sức chứa của phòng", null);
                        return BadRequest(response1);
                    }



                    // Check area name duplicates
                    var areaNameList = room.Areas.Select(x => x.AreaName).ToList();
                    foreach (var area in areaAdds)
                    {
                        if (areaNameList.Contains(area.AreaName))
                        {
                            var response1 = new ResponseDTO<object>(400, "Tên khu vực đang nhập trùng nhau hoặc đã tồn tại trong database", null);
                            return BadRequest(response1);
                        }
                        areaNameList.Add(area.AreaName);
                    }


                    // Thêm area

                    var areas = _mapper.Map<List<Area>>(areaAdds);
                    foreach (var area in areas)
                    {
                        room.Areas.Add(area);
                    }

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
                    if(room.IsDeleted == false)
                    room.IsDeleted = true;
                    await _roomService.Update(room);
                    return Ok("Thêm khu vực thành công!");
                }
                catch (Exception ex)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Lỗi truyền tham số khu vực!", null));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi thêm khu vực!", null));
            }
        }

        [HttpPatch("area")]
        public async Task<IActionResult> RemoveArea(int areaid)
        {
            try
            {
                var area = await _areaService.Get(x => x.AreaId == areaid);
                if (area == null)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Khu vực không tồn tại", null));
                }
                var faciInArea = await _usingFaclytyService.GetAll(x => x.AreaId == areaid);
                if (faciInArea.Any())
                {
                    return BadRequest(new ResponseDTO<object>(400, "Trong phòng đang có thiết bị. Nếu muốn xóa bạn phải xóa hết thiết vị trong phòng", null));
                }
                area.IsAvail = false;
                await _areaService.Update(area);
                return Ok("Xóa thành công!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi xóa khu vực!", null));
            }
        }
        [HttpDelete("faciremoveall")]
        public async Task<IActionResult> RemoveFaciFromArea(int areaid)
        {
            try
            {
                var area = await _areaService.Get(x => x.AreaId == areaid);
                if (area == null)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Khu vực không tồn tại", null));
                }
                var faciInArea = await _usingFaclytyService.GetAll(x => x.AreaId == areaid);
               await _usingFaclytyService.Delete(faciInArea);
                return Ok("Xóa thành công!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi xóa trang thiết bị!", null));
            }

        }


        [HttpGet("areainroomformanagement")]
        public async Task<IActionResult> GetAreasManagementInRoom(int roomId)
        {
            try
            {
                var room = await _roomService.Get(x => x.RoomId == roomId);
                if (room == null)
                {
                    var response = new ResponseDTO<object>(400, "Bạn nhập phòng không tồn tại", null);
                    return BadRequest(response);

                }
                var listAreaType = await _areaTypeService.GetAll();
                var listAreaTypeCategory = await _areaTypeCategoryService.GetAll();
                List<AreaGetForManagement> areaDTOs = new List<AreaGetForManagement>();
                IEnumerable<UsingFacility> usingFacilities = await _usingFaclytyService.GetAllWithInclude(x => x.Facility);

                foreach (var are in room.Areas)
                {
                    IEnumerable<UsingFacility> usingFacilities1 = new List<UsingFacility>();
                    if (usingFacilities != null)
                    {
                        usingFacilities1 =  usingFacilities.AsQueryable().Where(x => x.AreaId == are.AreaId);
                    }
                    var areaType = listAreaType.FirstOrDefault(x => x.AreaTypeId == are.AreaTypeId);
                    are.AreaType = areaType;
                    var areaTypeCategory = listAreaTypeCategory.FirstOrDefault(x => x.CategoryId == are.AreaType.AreaCategory);
                    if (!usingFacilities1.Any())
                    {
                        int faci = 0;
                        AreaGetForManagement areaGetForManagement = new AreaGetForManagement()
                        {
                            AreaId = are.AreaId,
                            AreaName = are.AreaName,
                            AreaTypeId = are.AreaTypeId,
                            AreaTypeName = are.AreaType.AreaTypeName,
                            CategoryId = are.AreaType.AreaCategory,
                            Title = are.AreaType.AreaTypeCategory.Title,
                            FaciAmount = faci,
                            IsAvail = are.IsAvail,
                            Size = are.AreaType.Size


                        };
                        areaDTOs.Add(areaGetForManagement);
                    }
                    else
                    {
                        int faci = 0;
                        foreach (var facility in usingFacilities1)
                        {
                            if (facility.Facility.FacilityCategory == 1)
                                faci += facility.Facility.Size*facility.Quantity;
                        }
                        AreaGetForManagement areaGetForManagement = new AreaGetForManagement()
                        {
                            AreaId = are.AreaId,
                            AreaName = are.AreaName,
                            AreaTypeId = are.AreaTypeId,
                            AreaTypeName = are.AreaType.AreaTypeName,
                            CategoryId = are.AreaType.AreaCategory,
                            Title = are.AreaType.AreaTypeCategory.Title,
                            FaciAmount = faci,
                            IsAvail = are.IsAvail,
                            Size = are.AreaType.Size


                        };
                        areaDTOs.Add(areaGetForManagement);
                    }
                }


                var response1 = new ResponseDTO<object>(200, "Danh sách khu vực", areaDTOs);
                return Ok(response1);
            }
            catch (Exception ex)
            {
                var response1 = new ResponseDTO<object>(500, "Lỗi lấy danh sách khu vực!", null);
                return StatusCode(500, response1);
            }

        }

    }
}
