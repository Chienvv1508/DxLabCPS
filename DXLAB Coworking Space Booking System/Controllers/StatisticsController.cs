using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/statistic")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;
        private readonly IMapper _mapper;

        public StatisticsController(IStatisticsService statisticsService, IMapper mapper)
        {
            _statisticsService = statisticsService;
            _mapper = mapper;
        }

        [HttpGet("student-group")]
        public async Task<IActionResult> GetRevenueByStudentGroup([FromQuery] PeriodRequestDTO request)
        {
            try
            {
                string period = request.Period?.ToLower() ?? "tháng";
                if (!new[] { "tuần", "tháng", "năm" }.Contains(period))
                {
                    return BadRequest(new ResponseDTO<object>(400, "Period không hợp lệ, phải là 'tuần', 'tháng', hoặc 'năm'!", null));
                }

                // Kiểm tra tham số theo từng period
                if (period == "tuần")
                {
                    if (!request.Year.HasValue || !request.Month.HasValue || !request.Week.HasValue)
                        return BadRequest(new ResponseDTO<object>(400, "Cần cung cấp Year, Month, và Week cho period 'tuần'!", null));
                    if (request.Week < 1 || request.Week > 5)
                        return BadRequest(new ResponseDTO<object>(400, "Tuần phải từ 1 đến 5!", null));
                }
                else if (period == "tháng")
                {
                    if (!request.Year.HasValue || !request.Month.HasValue)
                        return BadRequest(new ResponseDTO<object>(400, "Cần cung cấp Year và Month cho period 'tháng'!", null));
                }
                else if (period == "năm")
                {
                    if (!request.Year.HasValue)
                        return BadRequest(new ResponseDTO<object>(400, "Cần cung cấp Year cho period 'năm'!", null));
                }

                // Kiểm tra giá trị hợp lệ
                if (request.Year.HasValue && (request.Year < 2000 || request.Year > DateTime.Now.Year + 1))
                    return BadRequest(new ResponseDTO<object>(400, "Năm không hợp lệ!", null));
                if (request.Month.HasValue && (request.Month < 1 || request.Month > 12))
                    return BadRequest(new ResponseDTO<object>(400, "Tháng phải từ 1 đến 12!", null));

                var result = await _statisticsService.GetRevenueByStudentGroup(period, request.Year, request.Month, request.Week);
                return Ok(new ResponseDTO<StudentRevenueDTO>(200, "Lấy thành công doanh số đến từ sinh viên đặt!", result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseDTO<object>(400, $"Yêu cầu không hợp lệ: {ex.Message}", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi lấy dữ liệu: {ex.Message}", null));
            }
        }
    }
}