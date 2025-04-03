using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;   
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        public AreaController(IAreaService areaService, IUsingFacilytyService usingFacilytyService, IFaciStatusService faciStatusService, IMapper mapper, IFacilityService facilityService, IRoomService roomService)
        {
            _usingFaclytyService = usingFacilytyService;
            _facilityStatusService = faciStatusService;
            _areaService = areaService;
            _mapper = mapper;
            _facilityService = facilityService;
            _roomService = roomService;
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
                int numberOfPosition = 0;
                foreach (var faci in usingFacilities)
                {
                    if (faci.Facility.FacilityCategory == 1)
                    {
                        numberOfPosition += faci.Facility.Size;
                    }
                }

                var fullInfoOfFaci = await _facilityService.Get(x => x.FacilityId == faciAddDTO.FacilityId && x.BatchNumber == faciAddDTO.BatchNumber
                && x.ImportDate == faciAddDTO.ImportDate);
                if (fullInfoOfFaci == null)
                {
                    var reponse = new ResponseDTO<object>(400, "Thông tin thiết bị nhập sai. Vui lòng nhập lại", null);
                    return BadRequest(reponse);
                }
                if (fullInfoOfFaci.FacilityCategory == 1)
                {
                    if (areaInRoom.AreaType.Size - numberOfPosition < faciAddDTO.Quantity)
                    {
                        var reponse = new ResponseDTO<object>(400, "Bạn đã nhập quá sức chứa của phòng", null);
                        return BadRequest(reponse);
                    }
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

                var newUsingFacility = new UsingFacility
                {
                    AreaId = areaid,
                    BatchNumber = faciAddDTO.BatchNumber,
                    FacilityId = faciAddDTO.FacilityId,

                    Quantity = faciAddDTO.Quantity,
                    ImportDate = faciAddDTO.ImportDate
                };

                await _usingFaclytyService.Add(newUsingFacility, status);




                var reponse1 = new ResponseDTO<object>(200, "Thêm thiết bị thành công", null);
                return Ok(reponse1);
            }
            catch (Exception ex)
            {
                var reponse = new ResponseDTO<object>(400, "Thêm thiết bị lỗi", null);
                return BadRequest(reponse);
            }

        }
        [HttpGet("faciall")]
        public async Task<IActionResult> GetAllFaci()
        {
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
            catch(Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi database", null));
            }
           
        }

        [HttpGet("areainroom")]
        public async Task<IActionResult> GetAreasInRoom(int roomId)
        {
            var room = await _roomService.Get(x => x.RoomId == roomId);
            if(room == null)
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
            var faciInArea = await _usingFaclytyService.GetAllWithInclude(x => x.AreaId == areaid, x =>x.Facility);
            if(faciInArea == null)
            {
                return BadRequest(new ResponseDTO<object>(400, "Không có thiết bị nào trong khu vực", null));
            }
            List<FaciGetInAreaDTO> result = _mapper.Map<List<FaciGetInAreaDTO>>(faciInArea);
                
            return Ok(new ResponseDTO<object>(200, "Danh sách thiết bị: ", result));
        }
      
    }
}
