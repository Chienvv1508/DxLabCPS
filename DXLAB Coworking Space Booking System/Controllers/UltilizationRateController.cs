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
        private readonly ISlotService _slotService;
        private readonly IAreaTypeCategoryService _areaTypeCategoryService;

        public UltilizationRateController(IUltilizationRateService ultilizationRateService, IBookingDetailService bookingDetailService, ISlotService slotService, IAreaTypeCategoryService areaTypeCategoryService)
        {
            _bookingDetailService = bookingDetailService;
            _ultilizationRateService = ultilizationRateService;
            _slotService = slotService;
            _areaTypeCategoryService = areaTypeCategoryService;
        }

        [HttpPost]
        public async Task<IActionResult> THUltilizationRate()
        {
            try
            {
                int checkTime = int.Parse(DateTime.Now.ToString("HHmmss"));
                if (checkTime >= 180000)
                {
                    DateTime firtPara = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                    DateTime secondPara = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 18, 0, 0);
                    var bookingdetailList = await _bookingDetailService.GetAll(x => x.CheckinTime >= firtPara && x.CheckinTime <= secondPara);
                    var bookingdetailgroup = bookingdetailList.GroupBy(x => x.AreaId);
                    var slots = await _slotService.GetAll();
                    int numberOfSlot = slots.Count();
                    List<UltilizationRate> ultilizationRates = new List<UltilizationRate>();
                    foreach (var item in bookingdetailgroup)
                    {
                        var area = await _areaService.GetWithInclude(x => x.AreaId == item.Key, x => x.AreaType, x => x.Room);
                        if (area != null)
                        {
                            decimal rate = 0;
                            if (area.AreaType.AreaCategory == 1)
                            {
                                int m = area.AreaType.Size * numberOfSlot;
                                rate = Math.Round((decimal)item.Count() / m, 2);
                            }
                            else
                            {
                                rate = Math.Round((decimal)item.Count() / numberOfSlot, 2);
                            }

                            int roomid = area.RoomId;
                            string roomName = area.Room.RoomName;
                            int areaTypeId = area.AreaType.AreaTypeId;
                            string areaTypeName = area.AreaType.AreaTypeName;
                            int areaTypeCategoryId = area.AreaType.AreaCategory;
                            var areaTypeCategory = await _areaTypeCategoryService.Get(x => x.CategoryId == areaTypeCategoryId);
                            string areaTypeCategoryName = "";
                            if (areaTypeCategory != null)
                                areaTypeCategoryName = areaTypeCategory.Title;
                            var newUlRate = new UltilizationRate()
                            {
                                AreaId = area.AreaId,
                                AreaName = area.AreaName,
                                AreaTypeCategoryId = areaTypeCategoryId,
                                AreaTypeCategoryTitle = areaTypeCategoryName,
                                AreatypeId = areaTypeId,
                                AreaTypeName = areaTypeName,
                                RoomId = roomid,
                                RoomName = roomName
                            };
                            ultilizationRates.Add(newUlRate);

                        }
                    }
                    await _ultilizationRateService.Add(_ultilizationRateService);
                    return Ok();
                }
                else
                    return BadRequest("Chỉ tổng hợp sau 18h!");
            }
            catch(Exception ex)
            {
                return BadRequest();
            }

           
        }

        //[HttpGet("date")]
        //public async Task<IAction>
    }
}
