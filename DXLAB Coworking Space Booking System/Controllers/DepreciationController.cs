using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/depreciation")]
    [ApiController]
    public class DepreciationController : ControllerBase
    {
        private readonly IDepreciationService _depreciationService;
        private readonly IFacilityService _facilityService;

        public DepreciationController(IDepreciationService depreciationService, IFacilityService facilityService)
        {
            _depreciationService = depreciationService;
            _facilityService = facilityService;
        }

        [HttpPost]
        public async Task<IActionResult> THDepreciation()
        {
            try
            {
                var isTH = await _depreciationService.Get(x => x.SumDate.Year == DateTime.Now.Year && x.SumDate.Month == DateTime.Now.Month);
                if (isTH != null) return Ok("Đã tổng hợp khấu hao cho tháng này");
                var faciKHList = await _facilityService.GetAll(x => x.RemainingValue > 0);
                    await _facilityService.Update(faciKHList);
                    return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("month")]
        public async Task<IActionResult> GetInMonth(int year, int month)
        {
            try
            {
                if (year > 9999 || year < 2000 || month > 12 || month < 1)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Nhập năm hoặc tháng không hợp lệ!", null));
                }
                DateTime date = new DateTime(year, month, 1);
                var sums = await _depreciationService.GetAllWithInclude(x => x.SumDate.Year == date.Year && x.SumDate.Month == date.Month, x => x.Facility);
                List<DepreciationDTO> list = new List<DepreciationDTO>();
                foreach (var d in sums)
                {
                    DepreciationDTO depreciationDTO = new DepreciationDTO()
                    {
                        DepreciationSumId = d.DepreciationSumId,
                        FacilityId = d.FacilityId,
                        FacilityTitle = d.Facility.FacilityTitle,
                        FacilityCategory = d.Facility.FacilityCategory,
                        SumDate = d.SumDate,
                        DepreciationAmount = d.DepreciationAmount,

                        BatchNumber = d.BatchNumber

                    };
                    list.Add(depreciationDTO);
                }
                return Ok(new ResponseDTO<object>(200, "Lấy dữ liệu thành công", list));
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }
        [HttpGet("year")]
        public async Task<IActionResult> GetInYear(int year)
        {
            try
            {
                if (year > 9999 || year < 2000)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Nhập năm không hợp lệ!", null));
                }

                DateTime date = new DateTime(year, 1, 1);
                if (date.Year < DateTime.Now.Year || (date.Year == DateTime.Now.Year))
                {
                    var sums = await _depreciationService.GetAllWithInclude(x => x.SumDate.Year == date.Year, x => x.Facility);
                    List<DepreciationDTO> list = new List<DepreciationDTO>();
                    foreach (var d in sums)
                    {
                        DepreciationDTO depreciationDTO = new DepreciationDTO()
                        {
                            DepreciationSumId = d.DepreciationSumId,
                            FacilityId = d.FacilityId,
                            FacilityTitle = d.Facility.FacilityTitle,
                            FacilityCategory = d.Facility.FacilityCategory,
                            SumDate = d.SumDate,
                            DepreciationAmount = d.DepreciationAmount,
                            BatchNumber = d.BatchNumber

                        };
                        list.Add(depreciationDTO);
                    }    
                    return Ok(new ResponseDTO<object>(200, "Lấy dữ liệu thành công", list));
                }
                else
                {
                    return BadRequest(new ResponseDTO<object>(400, "Chưa tổng hợp khấu hao cho năm này!", null));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }
    }
}
