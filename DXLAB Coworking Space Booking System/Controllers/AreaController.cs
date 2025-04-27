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
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddFaciToArea(int areaid, int status, FaciAddDTO faciAddDTO)
        {
            try
            {
               
                // item 1: check item2: lỗi, item3: areaInroom
                Tuple<bool, string, Area> checkAreaAndStatus = await CheckExistedAreaAndStatus(areaid, status);
                if (checkAreaAndStatus.Item1 == false)
                {
                    var reponse = new ResponseDTO<object>(400, checkAreaAndStatus.Item2, null);
                    return BadRequest(reponse);
                }
                var areaInRoom = checkAreaAndStatus.Item3;

                var fullInfoOfFaci = await _facilityService.Get(x => x.FacilityId == faciAddDTO.FacilityId && x.BatchNumber == faciAddDTO.BatchNumber
                && x.ImportDate == faciAddDTO.ImportDate);
                if (fullInfoOfFaci == null)
                {
                    var reponse = new ResponseDTO<object>(400, "Thông tin thiết bị nhập sai. Vui lòng nhập lại", null);
                    return BadRequest(reponse);

                }
                // item1: addable, item2: isfull, item3: mã lỗi
                Tuple<bool, bool, string> checkIsAddable = await checkIsAddableAndStatusAfterAdd(fullInfoOfFaci, areaid, areaInRoom, faciAddDTO);
                if (checkIsAddable.Item1 == false)
                {
                    var reponse = new ResponseDTO<object>(400, checkIsAddable.Item3, null);
                    return BadRequest(reponse);
                }

                //check lượng đang có trong status

                Tuple<bool, string> checkIsAvailFaciForAdd = await CheckIsAvailFaciForAdd(faciAddDTO, status);
                if (checkIsAvailFaciForAdd.Item1 == false)
                {
                    var reponse = new ResponseDTO<object>(400, checkIsAvailFaciForAdd.Item2, null);
                    return BadRequest(reponse);
                }

                //Thêm đoạn check xem trong phòng có using này chưa. Nếu có thì cộng nếu không thì thêm mới

                var existedFaciInArea = await _usingFaclytyService.Get(x => x.AreaId == areaid && x.BatchNumber == faciAddDTO.BatchNumber

                && x.FacilityId == faciAddDTO.FacilityId && x.ImportDate == faciAddDTO.ImportDate);
                bool statusOfArea = checkIsAddable.Item2;
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

                return Ok(new ResponseDTO<object>(200, "Thêm thiết bị thành công", null));
            }
            catch (Exception ex)
            {
                var reponse = new ResponseDTO<object>(400, "Thêm thiết bị lỗi", null);
                return BadRequest(reponse);
            }

        }

        private async Task<Tuple<bool, string>> CheckIsAvailFaciForAdd(FaciAddDTO faciAddDTO, int status)
        {
            try
            {
                var faciInStatus = await _facilityStatusService.Get(x => x.FacilityId == faciAddDTO.FacilityId && x.BatchNumber == faciAddDTO.BatchNumber
                && x.ImportDate == faciAddDTO.ImportDate && x.Status == status);

                if (faciInStatus == null)
                {
                    string tt = status == 0 ? "Mới" : "Đã sử dụng";
                    return new Tuple<bool, string>(false, $"Với trạng thái {tt} hiện không có thiết bị này");
                }
                if (faciInStatus.Quantity < faciAddDTO.Quantity)
                {
                    string tt = status == 0 ? "Mới" : "Đã sử dụng";
                    return new Tuple<bool, string>(false, $"Với trạng thái {tt} hiện không có đủ {faciAddDTO.Quantity} thiết bị");
                }
                return new Tuple<bool, string>(true, "");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, "");
            }
        }

        private async Task<Tuple<bool, bool, string>> checkIsAddableAndStatusAfterAdd(Facility fullInfoOfFaci, int areaid, Area areaInRoom, FaciAddDTO faciAddDTO)
        {
            try
            {
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

                if (fullInfoOfFaci.FacilityCategory == 1)
                {
                    if (areaInRoom.AreaType.Size - numberOfPositionT < faciAddDTO.Quantity * fullInfoOfFaci.Size)
                    {
                        return new Tuple<bool, bool, string>(false, false, "Bạn đã nhập quá số lượng bàn cho phép của phòng");
                    }
                    if (areaInRoom.AreaType.Size - numberOfPositionT == faciAddDTO.Quantity * fullInfoOfFaci.Size)
                        isFullT = true;
                    if (areaInRoom.AreaType.Size == numberOfPositionCh)
                        isFullCh = true;
                }
                // thêm đoạn này
                if (fullInfoOfFaci.FacilityCategory == 0)
                {
                    if (areaInRoom.AreaType.Size - numberOfPositionCh < faciAddDTO.Quantity)
                    {
                        return new Tuple<bool, bool, string>(false, false, "Bạn đã nhập quá số lượng ghế cho phép của phòng");
                    }
                    if (areaInRoom.AreaType.Size - numberOfPositionCh == faciAddDTO.Quantity)
                        isFullCh = true;
                    if (areaInRoom.AreaType.Size == numberOfPositionT)
                        isFullT = true;
                }

                return new Tuple<bool, bool, string>(true, isFullT && isFullCh, "");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, bool, string>(false, false, "");
            }
        }

        private async Task<Tuple<bool, string, Area>> CheckExistedAreaAndStatus(int areaid, int status)
        {
            try
            {
                var areaInRoom = await _areaService.GetWithInclude(x => x.AreaId == areaid && x.Status != 2, x => x.AreaType);
                if (areaInRoom == null)
                {
                    return new Tuple<bool, string, Area>(false, "Không tìm thấy khu vực. Vui lòng nhập lại khu vực", null);
                }
                if (areaInRoom.Status == 1)
                {
                    return new Tuple<bool, string, Area>(false, "Khu vực đã đầy thiết bị. Không thêm được vào phòng!", null);
                }
                //0-mơi 1-dasudung 2--hong
                if (status < 0 && status > 1)
                {
                    return new Tuple<bool, string, Area>(false, "Thiết bị được nhập phải mới hoặc vẫn sử dụng được", null);
                }
                return new Tuple<bool, string, Area>(true, "", areaInRoom);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, Area>(false, ex.Message, null);
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
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveFaciFromArea([FromBody] RemoveFaciDTO removedFaciDTO)
        {
            try
            {
                var existedFaciInArea = await _usingFaclytyService.Get(x => x.FacilityId == removedFaciDTO.FacilityId &&
                 x.AreaId == removedFaciDTO.AreaId && x.BatchNumber == removedFaciDTO.BatchNumber && x.ImportDate == removedFaciDTO.ImportDate
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
        [HttpPost("faciremovereport")]
        public async Task<IActionResult> GetAllBrokenFaciReport([FromBody] BrokernFaciReportDTO removedFaciDTO)
        {

            var result  = await _usingFaclytyService.GetAllBrokenFaciFromReport(removedFaciDTO);
            if(result.StatusCode == 200)
            {
                var usingFacilityDTOs = _mapper.Map<List<UsingFacilityDTO>>(result.Data);

                return Ok(new ResponseDTO<object>(200, "Lấy thành công danh sách thiết bị cần xóa", usingFacilityDTOs));
            }
            return StatusCode(result.StatusCode,result);
        }

        [HttpGet("areainroom")]
        public async Task<IActionResult> GetAreasInRoom(int roomId)
        {
            var room = await _roomService.Get(x => x.RoomId == roomId && x.Status != 2);
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
                    var room = await _roomService.Get(x => x.RoomId == roomId && DateTime.Now.Date < x.ExpiredDate);
                    if (room == null)
                    {
                        return BadRequest(new ResponseDTO<object>(400, "Lỗi phòng không tồn tại!", null));
                    }

                    var areaTypeList = await _areaTypeService.GetAll(x => x.Status == 1);

                    Tuple<bool, int, Area, string> totalSizeAndIndividualArea = GetToTalSizeAndIndividual(room, areaTypeList);
                    if (totalSizeAndIndividualArea.Item1 == false)
                    {
                        var response1 = new ResponseDTO<object>(400, totalSizeAndIndividualArea.Item4, null);
                        return BadRequest(response1);
                    }

                    int totalSize = totalSizeAndIndividualArea.Item2;
                    Area individualArea = totalSizeAndIndividualArea.Item3 as Area;

                    if (totalSize == room.Capacity)
                    {
                        var response1 = new ResponseDTO<object>(400, "Phòng đã đầy sức chứa. Không thể thêm khu vực", null);
                        return BadRequest(response1);
                    }


                    Tuple<bool, List<AreaType>, int, string> checkValidAreaInput = CheckValidAreaInput(areaAdds, areaTypeList);
                    if (checkValidAreaInput.Item1 == false)
                    {
                        var response1 = new ResponseDTO<object>(400, checkValidAreaInput.Item4, null);
                        return BadRequest(response1);
                    }
                    List<AreaType> areaTypeInputs = checkValidAreaInput.Item2;
                    totalSize += checkValidAreaInput.Item3;
                    if (totalSize > room.Capacity)
                    {
                        var response1 = new ResponseDTO<object>(400, "Bạn đã nhập quá sức chứa của phòng", null);
                        return BadRequest(response1);
                    }

                    if (areaTypeInputs.FirstOrDefault(x => x.AreaCategory == 1) != null && individualArea != null)
                    {
                        var response1 = new ResponseDTO<object>(400, "Trong phòng đã có loại cá nhân không thêm được loại cá nhân nữa!", null);
                        return BadRequest(response1);

                    }

                    Tuple<bool, string> checkDuplicateNameOfArea = CheckDuplicateNameOfArea(room, areaAdds);
                    if (checkDuplicateNameOfArea.Item1 == false)
                    {
                        var response1 = new ResponseDTO<object>(400, checkDuplicateNameOfArea.Item2, null);
                        return BadRequest(response1);
                    }

                    Tuple<bool, string> addArea = AddArea(room, areaAdds, individualArea);
                    if (addArea.Item1 == false)
                    {
                        var response1 = new ResponseDTO<object>(400, addArea.Item2, null);
                        return BadRequest(response1);

                    }
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

        private Tuple<bool, string> AddArea(Room room, List<AreaAdd> areaAdds, Area individualArea)
        {
            try
            {
                if (room == null && areaAdds == null)
                    return new Tuple<bool, string>(false, "Lỗi nhập dữ liệu đầu vào!");
                var areas = _mapper.Map<List<Area>>(areaAdds);
                foreach (var area in areas)
                {
                    area.ExpiredDate = new DateTime(3000, 1, 1);
                    room.Areas.Add(area);
                }

                if (individualArea != null)
                {
                    var xr = room.Areas.FirstOrDefault(x => x.AreaTypeId == individualArea.AreaTypeId && x.ExpiredDate.Date > DateTime.Now.Date);
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
                return new Tuple<bool, string>(true, "");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, "Lỗi thêm khu vực vào phòng!");
            }

        }

        private Tuple<bool, string> CheckDuplicateNameOfArea(Room room, List<AreaAdd> areaAdds)
        {
            try
            {
                if (room == null && areaAdds == null)
                    return new Tuple<bool, string>(false, "Lỗi nhập dữ liệu đầu vào!");
                var areaNameList = room.Areas.Where(x => x.ExpiredDate.Date > DateTime.Now.Date).Select(x => x.AreaName).ToList();
                foreach (var area in areaAdds)
                {
                    if (areaNameList.Contains(area.AreaName))
                    {

                        return new Tuple<bool, string>(false, "Tên khu vực đang nhập trùng nhau hoặc đã tồn tại trong database");
                    }
                    areaNameList.Add(area.AreaName);
                }
                return new Tuple<bool, string>(true, "");

            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, "Lỗi kiểm tra trùng tên của dữ liệu nhập vào");
            }
        }

        private Tuple<bool, List<AreaType>, int, string> CheckValidAreaInput(List<AreaAdd> areaAdds, IEnumerable<AreaType> areaTypeList)
        {
            try
            {
                if (areaAdds == null || areaTypeList == null)
                {
                    return new Tuple<bool, List<AreaType>, int, string>(false, null, 0, "Lỗi nhập thông tin thêm khu vực!");
                }
                int countIndividual = 0;
                int size = 0;
                List<AreaType> areaTypesInput = new List<AreaType>();
                foreach (var area in areaAdds)
                {
                    var newArea = areaTypeList.FirstOrDefault(x => x.AreaTypeId == area.AreaTypeId && x.Status == 1);

                    if (newArea == null)
                    {
                        return new Tuple<bool, List<AreaType>, int, string>(false, null, 0, "Nhập sai id của loại khu vực!");
                    }
                    size += newArea.Size;
                    if (newArea.AreaCategory == 1) countIndividual++;
                    if (countIndividual > 1)
                        return new Tuple<bool, List<AreaType>, int, string>(false, null, 0, "Chỉ được nhập 1 loại khu vực cá nhân!");
                    areaTypesInput.Add(newArea);
                }
                return new Tuple<bool, List<AreaType>, int, string>(true, areaTypesInput, size, "");

            }
            catch (Exception ex)
            {
                return new Tuple<bool, List<AreaType>, int, string>(false, null, 0, "");
            }
        }

        private Tuple<bool, int, Area, string> GetToTalSizeAndIndividual(Room room, IEnumerable<AreaType> areaTypeList)
        {
            try
            {
                if (room != null && areaTypeList != null)
                {
                    int areas_totalSize = 0;
                    Area individualArea = null;
                    //int countIndividualAre = 0;


                    foreach (var area in room.Areas.Where(x => x.ExpiredDate < DateTime.Now.Date))
                    {
                        var areatype = areaTypeList.FirstOrDefault(x => x.AreaTypeId == area.AreaTypeId && x.Status == 1);
                        if (areatype != null)
                        {
                            areas_totalSize += areatype.Size;
                            if (areatype.AreaCategory == 1)
                            {
                                //countIndividualAre++;
                                individualArea = area;
                                individualArea.AreaType = areatype;
                            }
                            area.AreaType = areatype;
                        }

                    }
                    return new Tuple<bool, int, Area, string>(true, areas_totalSize, individualArea, "");
                }
                else
                    return new Tuple<bool, int, Area, string>(false, 0, null, "khu vực và loại khu vực không để bỏ trống");

            }
            catch (
            Exception ex)
            {
                return new Tuple<bool, int, Area, string>(false, 0, null, "Lỗi không thêm được khu vực vào phòng!");


            }
        }

        [HttpPatch("area")]
        public async Task<IActionResult> RemoveArea(int areaid, DateTime expiredDate)
        {
            try
            {
                if(expiredDate <= DateTime.Now.Date.AddDays(14))
                    return BadRequest(new ResponseDTO<object>(400, "Phải để ngày hết hạn lớn hơn 14 ngày từ ngày hiện tại!", null));
                var area = await _areaService.Get(x => x.AreaId == areaid && x.ExpiredDate.Date > DateTime.Now.Date);
                if (area == null)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Khu vực không tồn tại", null));
                }
                var faciInArea = await _usingFaclytyService.GetAll(x => x.AreaId == areaid);
                if (faciInArea.Any())
                {
                    return BadRequest(new ResponseDTO<object>(400, "Trong phòng đang có thiết bị. Nếu muốn xóa bạn phải xóa hết thiết vị trong phòng", null));
                }
                area.ExpiredDate = expiredDate;
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
                var area = await _areaService.Get(x => x.AreaId == areaid && x.Status != 2);
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
                var room = await _roomService.Get(x => x.RoomId == roomId && x.Status != 2);
                if (room == null)
                {
                    var response = new ResponseDTO<object>(400, "Bạn nhập phòng không tồn tại", null);
                    return BadRequest(response);

                }
                var listAreaType = await _areaTypeService.GetAll(x => x.Status == 1);
                var listAreaTypeCategory = await _areaTypeCategoryService.GetAll(x => x.Status == 1);
                List<AreaGetForManagement> areaDTOs = new List<AreaGetForManagement>();
                IEnumerable<UsingFacility> usingFacilities = await _usingFaclytyService.GetAllWithInclude(x => x.Facility);

                foreach (var are in room.Areas.Where(x => x.ExpiredDate.Date > DateTime.Now.Date))
                {
                    IEnumerable<UsingFacility> usingFacilities1 = new List<UsingFacility>();
                    if (usingFacilities != null)
                    {
                        usingFacilities1 = usingFacilities.AsQueryable().Where(x => x.AreaId == are.AreaId);
                    }
                    var areaType = listAreaType.FirstOrDefault(x => x.AreaTypeId == are.AreaTypeId);
                    are.AreaType = areaType;
                    var areaTypeCategory = listAreaTypeCategory.FirstOrDefault(x => x.CategoryId == are.AreaType.AreaCategory);
                    //if (!usingFacilities1.Any())
                    //{
                    //    int faci = 0;
                    //    int faciCh = 0;
                    //    AreaGetForManagement areaGetForManagement = new AreaGetForManagement()
                    //    {
                    //        AreaId = are.AreaId,
                    //        AreaName = are.AreaName,
                    //        AreaTypeId = are.AreaTypeId,
                    //        AreaTypeName = are.AreaType.AreaTypeName,
                    //        CategoryId = are.AreaType.AreaCategory,
                    //        Title = are.AreaType.AreaTypeCategory.Title,
                    //        FaciAmount = faci,
                    //        FaciAmountCh = faciCh,
                    //        Status = are.Status,
                    //        Size = are.AreaType.Size


                    //    };
                    //    areaDTOs.Add(areaGetForManagement);
                    //}
                    //else
                    //{
                        int faci = 0;
                        int faciCh = 0;
                        foreach (var facility in usingFacilities1)
                        {
                            if (facility.Facility.FacilityCategory == 1)
                                faci += facility.Facility.Size * facility.Quantity;
                            else
                                faciCh += facility.Quantity;
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
                            FaciAmountCh = faciCh,
                            Status = are.Status,
                            Size = are.AreaType.Size,
                            ExpiredDate = are.ExpiredDate
                            


                        };
                        areaDTOs.Add(areaGetForManagement);
                    //}
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
