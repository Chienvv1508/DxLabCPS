using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/ultilizationrate")]
    [ApiController]
    public class UltilizationRateController : ControllerBase
    {
        private readonly IUltilizationRateService _ultilizationRateService;
        private readonly IBookingDetailService _bookingDetailService;
        private readonly IAreaService _areaService;

        public UltilizationRateController(IUltilizationRateService ultilizationRateService, IBookingDetailService bookingDetailService)
        {
            _bookingDetailService = bookingDetailService;
            _ultilizationRateService = ultilizationRateService;
        }

        [HttpPost]
        public async Task<IActionResult> THUltilizationRate()
        {
            int checkTime = int.Parse(DateTime.Now.ToString("HHmmss"));
            if (checkTime >= 180000)
            {
                DateTime firtPara = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                DateTime secondPara = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 18, 0, 0);
                var bookingdetailList = await _bookingDetailService.GetAll(x => x.CheckinTime >= firtPara && x.CheckinTime <= secondPara);
                var bookingdetailgroup = bookingdetailList.GroupBy(x => x.AreaId);
                return Ok();
            }
            else
                return BadRequest("Chỉ tổng hợp sau 18h!");
        }
    }
}
