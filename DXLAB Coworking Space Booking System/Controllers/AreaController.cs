using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreaController : ControllerBase
    {
        private readonly IAreaService _areaService;
        private readonly IUsingFacilytyService _usingFaclytyService;
        private readonly IFaciStatusService _facilityStatusService;
        private readonly IMapper _mapper;
        private readonly IFacilityService _facilityService;

        public AreaController(IAreaService areaService, IUsingFacilytyService usingFacilytyService, IFaciStatusService faciStatusService, IMapper mapper, IFacilityService facilityService)
        {
            _usingFaclytyService = usingFacilytyService;
            _facilityStatusService = faciStatusService;
            _areaService = areaService;
            _mapper = mapper;
            _facilityService = facilityService;
        }

        [HttpPost("faci")]
        public async Task<IActionResult> AddFaciToArea(int areaid, int status,FaciAddDTO faciAddDTO)
        {
            try
            {
                
                var areaInRoom = await _areaService.GetWithInclude(x => x.AreaId == areaid,x => x.AreaType);
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
                foreach(var faci in usingFacilities)
                {
                    if(faci.Facility.FacilityCategory == 1)
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
                if(fullInfoOfFaci.FacilityCategory == 1)
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

                if(faciInStatus == null)
                {
                    string tt = status == 0 ? "Mới" : "Đã sử dụng";
                    var reponse = new ResponseDTO<object>(400, $"Với trạng thái {tt} hiện không có thiết bị này", null);
                    return BadRequest(reponse);
                }
                if(faciInStatus.Quantity < faciAddDTO.Quantity)
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

                    await _usingFaclytyService.Add(newUsingFacility,status);
               
                


                var reponse1 = new ResponseDTO<object>(200, "Thêm thiết bị thành công", null);
                return Ok(reponse1);
            }
            catch(Exception ex)
            {
                var reponse = new ResponseDTO<object>(400, "Thêm thiết bị lỗi", null);
                return BadRequest(reponse);
            }
            
        }
        [HttpGet("getFaci")]
        public async Task<IActionResult> GetAllFaci()
        {
            try
            {
                var faciStatusList = await _facilityStatusService.GetAll(x => x.Status == 0 || x.Status == 1);
                var response = new ResponseDTO<object>(200, "Lấy thành công", faciStatusList);
                return Ok(response);
            }
            catch(Exception ex)
            {
                var response = new ResponseDTO<object>(500, "Lỗi DB!", null);
                return StatusCode(500, response);
                    
            }
            
        }
    }
}
