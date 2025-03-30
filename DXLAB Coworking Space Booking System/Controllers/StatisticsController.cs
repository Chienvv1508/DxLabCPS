using AutoMapper;
using DxLabCoworkingSpace;
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
        private readonly IMapper _mapper;

        public StatisticsController(IStatisticsService statisticsService, IMapper mapper)
        {
            _statisticsService = statisticsService;
            _mapper = mapper;
        }

        [HttpGet("student-group")]
        public async Task<IActionResult> GetRevenueByStudentGroup([FromQuery] PeriodRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ!", errors));
            }

            try
            {
                var result = await _statisticsService.GetRevenueByStudentGroup(request.Period.ToLower(), request.Year, request.Month, request.Week);
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