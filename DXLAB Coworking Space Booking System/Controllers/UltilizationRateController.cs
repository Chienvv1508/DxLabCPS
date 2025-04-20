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

        public UltilizationRateController(IUltilizationRateService ultilizationRateService, IBookingDetailService bookingDetailService, ISlotService slotService, IAreaTypeCategoryService areaTypeCategoryService, IAreaService areaService)
        {
            _bookingDetailService = bookingDetailService;
            _ultilizationRateService = ultilizationRateService;
            _slotService = slotService;
            _areaTypeCategoryService = areaTypeCategoryService;
            _areaService = areaService;
        }

        [HttpPost]
        public async Task<IActionResult> THUltilizationRate()
        {
            try
            {
                var isTH = await _ultilizationRateService.Get(x => x.THDate.Date == DateTime.Now.Date);
                if (isTH != null) return Ok("Đã tổng hợp cho ngày hôm nay");
                DateTime firtPara = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                DateTime secondPara = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 18, 0, 0);
                var bookingdetailList = await _bookingDetailService.GetAll(x => x.CheckinTime >= firtPara && x.CheckinTime <= secondPara);
                var bookingdetailgroup = bookingdetailList.GroupBy(x => x.AreaId);
                var slots = await _slotService.GetAll(x => x.Status == 1);
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
                            THDate = DateTime.Now,
                            AreaId = area.AreaId,
                            AreaName = area.AreaName,
                            AreaTypeCategoryId = areaTypeCategoryId,
                            AreaTypeCategoryTitle = areaTypeCategoryName,
                            AreatypeId = areaTypeId,
                            AreaTypeName = areaTypeName,
                            RoomId = roomid,
                            RoomName = roomName,
                            Rate = rate
                        };
                        ultilizationRates.Add(newUlRate);

                    }
                }
                var allArea = await _areaService.GetAllWithInclude(x => x.AreaType, x => x.Room);
                allArea = allArea.AsQueryable().Where(x => x.Status == 1);
                foreach (var area in allArea)
                {
                    var areaExisted = ultilizationRates.FirstOrDefault(x => x.AreaId == area.AreaId);
                    if (areaExisted == null)
                    {
                        decimal rate = 0;
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
                            THDate = DateTime.Now,
                            AreaId = area.AreaId,
                            AreaName = area.AreaName,
                            AreaTypeCategoryId = areaTypeCategoryId,
                            AreaTypeCategoryTitle = areaTypeCategoryName,
                            AreatypeId = areaTypeId,
                            AreaTypeName = areaTypeName,
                            RoomId = roomid,
                            RoomName = roomName,
                            Rate = rate
                        };
                        ultilizationRates.Add(newUlRate);
                    }
                }
                await _ultilizationRateService.Add(ultilizationRates);
                return Ok();
                //}
                //else
                //    return BadRequest("Chỉ tổng hợp sau 18h!");
            }
            catch (Exception ex)
            {
                return BadRequest();
            }


        }

        [HttpGet("date")]
        public async Task<IActionResult> GetRateOnDate(DateTime dateTime)
        {
            try
            {
                //Check ngày nhưng tạm thời bỏ để test
                var result = await _ultilizationRateService.GetAll(x => x.THDate.Date == dateTime.Date);
                var response = new ResponseDTO<object>(200, "Danh sách rate: ", result);
                return Ok(response);
            }
            catch
            {
                return StatusCode(500);
            }

        }

        [HttpGet("month")]
        public async Task<IActionResult> GetRateInMonth(int year, int month)
        {
            try
            {
                //Check ngày nhưng tạm thời bỏ để test
                if (year > 9999 || year < 2000 || month > 12 || month < 1)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Nhập năm hoặc tháng không hợp lệ!", null));
                }
                DateTime firstDate = new DateTime(year, month, 1);
                DateTime lastDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                var result = await _ultilizationRateService.GetAll(x => x.THDate.Date >= firstDate.Date && x.THDate.Date <= lastDate.Date);
                var response = new ResponseDTO<object>(200, "Danh sách rate: ", result);
                return Ok(response);
            }
            catch
            {
                return StatusCode(500);
            }

        }

        [HttpGet("year")]
        public async Task<IActionResult> GetRateInYear(int year)
        {
            try
            {
                //Check ngày nhưng tạm thời bỏ để test
                if (year > 9999 || year < 2000)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Nhập năm không hợp lệ!", null));
                }
                DateTime firstDate = new DateTime(year, 1, 1);
                DateTime lastDate = new DateTime(year, 12, DateTime.DaysInMonth(year, 12));
                var result = await _ultilizationRateService.GetAll(x => x.THDate.Date >= firstDate.Date && x.THDate.Date <= lastDate.Date);
                var response = new ResponseDTO<object>(200, "Danh sách rate: ", result);
                return Ok(response);
            }
            catch
            {
                return StatusCode(500);
            }

        }
    }
}
