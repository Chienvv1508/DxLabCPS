using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;
using System.Linq.Expressions;

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
        private readonly IBookingDetailService _bookingDetailService;

        public AreaController(IAreaService areaService, IUsingFacilytyService usingFacilytyService,
            IFaciStatusService faciStatusService, IMapper mapper, IFacilityService facilityService,
            IRoomService roomService, IAreaTypeService areaTypeService, IAreaTypeCategoryService areaTypeCategoryService, IBookingDetailService bookingDetailService)
        {
            _usingFaclytyService = usingFacilytyService;
            _facilityStatusService = faciStatusService;
            _areaService = areaService;
            _mapper = mapper;
            _facilityService = facilityService;
            _roomService = roomService;
            _areaTypeService = areaTypeService;
            _areaTypeCategoryService = areaTypeCategoryService;
            _bookingDetailService = bookingDetailService;
        }

        [HttpPost("faci")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddFaciToArea(int areaid, int status, FaciAddDTO faciAddDTO)
        {
            ResponseDTO<object> result = await _areaService.AddFaciToArea(areaid, status, faciAddDTO);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("allfacistatus")]
        public async Task<IActionResult> GetAllFaciStatus()
        { 
            // Mục đích cho admin chon faci thêm vào phòng
            try
            {
                var faciStatusList = await _facilityStatusService.GetAllWithInclude(x => ((x.Status == 0 || x.Status == 1) && x.Quantity > 0), x => x.Facility);
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
        public async Task<IActionResult> RemoveFaciFromArea([FromBody] RemoveFaciDTO removedFaciDTO)
        {

            ResponseDTO<object> result = await _areaService.RemoveFaciFromArea(removedFaciDTO);
            return StatusCode(result.StatusCode, result);

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

           ResponseDTO<Area> result = await _areaService.AddNewArea(roomId, areaAdds);
            return StatusCode(result.StatusCode, result);
        }  

        [HttpPut]
        [Route("area")]
        public async Task<IActionResult> SetExpiredTimeToArea(int areaId)
        {
            ResponseDTO<Area> result = await _areaService.SetExpiredTimeToArea(areaId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("area")]
        public async Task<IActionResult> RemoveArea(int areaid)
        {
            ResponseDTO<Area> result = await _areaService.RemoveArea(areaid);
            return StatusCode(result.StatusCode, result);
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
            ResponseDTO<object> result = await _areaService.GetAreasManagementInRoom(roomId);
            return StatusCode(result.StatusCode, result);
        }

    }
}
