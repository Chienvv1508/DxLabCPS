using AutoMapper;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/statistic")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet("student-group")]
        public async Task<IActionResult> GetDetailedRevenue([FromQuery] PeriodRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ!", errors));
            }

            try
            {
                var result = await _statisticsService.GetDetailedRevenue(request.Period.ToLower(), request.Year, request.Month);
                return Ok(new ResponseDTO<DetailedRevenueDTO>(200, "Lấy thành công doanh số chi tiết!", result));
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