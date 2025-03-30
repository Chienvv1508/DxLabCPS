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

        public AreaController(IAreaService areaService, IUsingFacilytyService usingFacilytyService, IFaciStatusService faciStatusService, IMapper mapper)
        {
            _usingFaclytyService = usingFacilytyService;
            _facilityStatusService = faciStatusService;
            _areaService = areaService;
            _mapper = mapper;
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
                if (status < 1 && status > 2)
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
                
                if(faciAddDTO.FaciCategory == 1  && (areaInRoom.AreaType.Size - numberOfPosition) < faciAddDTO.Quantity)
                {
                    var reponse = new ResponseDTO<object>(400, "Bạn đã nhập quá sức chứa của phòng", null);
                    return BadRequest(reponse);
                }

                    var existedFaci = await _facilityStatusService.Get(x => x.FailityId == faciAddDTO.FacilityId && x.Status == status && faciAddDTO.BatchNumber == x.BatchNumber);
                    if (existedFaci == null)
                    {
                        var reponse = new ResponseDTO<object>(400, "Thiết bị được nhập không tồn tại!", null);
                        return BadRequest(reponse);
                    }
                    if (existedFaci.Quantity <= 0)
                    {
                        var reponse = new ResponseDTO<object>(400, "Thiết bị hiện hết trong kho", null);
                        return BadRequest(reponse);
                    }
                    if (faciAddDTO.Quantity > existedFaci.Quantity)
                    {
                        var reponse = new ResponseDTO<object>(400, "Bạn đã nhập quá số lượng trong kho", null);
                        return BadRequest(reponse);
                    }

                var usingFaci = new UsingFacility 
                {AreaId = areaid, FacilityId = faciAddDTO.FacilityId, BatchNumber = faciAddDTO.BatchNumber
                 , Quantity = faciAddDTO.Quantity
                };
                   
                    var existedFaciStatus = await _facilityStatusService.Get(x => x.FailityId == faciAddDTO.FacilityId && x.Status == status && x.BatchNumber == faciAddDTO.BatchNumber);
                    existedFaci.Quantity -= faciAddDTO.Quantity;

                    await _usingFaclytyService.Add(usingFaci);
                    await _facilityStatusService.Update(existedFaci);
                


                var reponse1 = new ResponseDTO<object>(400, "Thêm thiết bị thành công", null);
                return Ok(reponse1);
            }
            catch(Exception ex)
            {
                var reponse = new ResponseDTO<object>(400, "Thêm thiết bị lỗi", null);
                return BadRequest(reponse);
            }
            
        }
    }
}
