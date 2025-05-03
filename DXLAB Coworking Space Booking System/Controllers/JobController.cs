using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/job")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly ILogger<JobController> _logger;
        private readonly IFacilityService _facilityService;
        private readonly ISumaryExpenseService _sumaryExpenseService;

        public JobController(ILogger<JobController> logger, IFacilityService facilityService,ISumaryExpenseService sumaryExpenseService)
        {
            _logger = logger;
            _facilityService = facilityService;
            _sumaryExpenseService = sumaryExpenseService;
        }

        [HttpPost("jobexpense")]
        public async Task<IActionResult> SumaryExpense([FromBody] THJobExpenseDTO tHJobExpenseDTO)
        {
            try
            {           
                _logger.LogInformation("Chạy job tổng hợp chi phí");
                //Check date xem ngày có hơp lệ ko
                if(tHJobExpenseDTO.dateSum > DateTime.Now)
                {
                    return BadRequest("Date không hợp lệ");
                }

                var isTH = await _sumaryExpenseService.Get(x => x.SumaryDate.Month == tHJobExpenseDTO.dateSum.Month && x.SumaryDate.Year == tHJobExpenseDTO.dateSum.Year);
                if (isTH != null) return Ok($"Đã tổng hợp cho tháng: {tHJobExpenseDTO.dateSum.Month}");
                DateTime sumDate = tHJobExpenseDTO.dateSum;

                DateTime firstDay = new DateTime(sumDate.Year, sumDate.Month, 1, 0, 0, 0);
                DateTime lastDay = new DateTime(sumDate.Year, sumDate.Month, DateTime.DaysInMonth(sumDate.Year, sumDate.Month), 23, 59, 59);

                var faciList = await _facilityService.GetAll(x => x.ImportDate >= firstDay && x.ImportDate <= lastDay);
                var sumGroup = faciList.GroupBy(x => x.FacilityCategory);
                List<SumaryExpense> sumaryExpenses = new List<SumaryExpense>();
                foreach( var group in sumGroup )
                {
                    decimal amount = 0;
                    foreach(var item in group)
                    {
                        amount += item.Cost*item.Quantity;
                    }
                    var sum = new SumaryExpense()
                    {
                        Amout = amount,
                        FaciCategory = group.Key,
                        SumaryDate = sumDate
                    };
                    sumaryExpenses.Add(sum);
                }
                await _sumaryExpenseService.Add(sumaryExpenses);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("month")]
        public async Task<IActionResult> GetSumaryExpense(int year, int month)
        {
            try
            {
                if(year > 9999 || year < 2000 || month >12 || month < 1)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Nhập năm hoặc tháng không hợp lệ!", null));
                }

                DateTime date = new DateTime(year, month, 1);
                    var sums = await _sumaryExpenseService.GetAll(x=>x.SumaryDate.Year == date.Year && x.SumaryDate.Month == date.Month);
                    return Ok(new ResponseDTO<object>(200, "Lấy dữ liệu thành công", sums));     
            }
            catch(Exception ex)
            {
                return StatusCode(500);
            }
            
        }
        [HttpGet("year")]
        public async Task<IActionResult> GetSumaryExpenseYear(int year)
        {
            try
            {
                if (year > 9999 || year < 2000 )
                {
                    return BadRequest(new ResponseDTO<object>(400, "Nhập năm không hợp lệ!", null));
                }

                DateTime date = new DateTime(year, 1, 1);
                var sums = await _sumaryExpenseService.GetAll(x => x.SumaryDate.Year == date.Year);
                    return Ok(new ResponseDTO<object>(200, "Lấy dữ liệu thành công", sums));
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }
    }
}
