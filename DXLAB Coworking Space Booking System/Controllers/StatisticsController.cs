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
                // Kiểm tra period hợp lệ
                string period = request.Period?.ToLower() ?? "tháng";
                if (!new[] { "tuần", "tháng", "năm" }.Contains(period))
                {
                    return BadRequest(new ResponseDTO<object>(400, "Period không hợp lệ, phải là 'tuần', 'tháng', hoặc 'năm'!", null));
                }

                var result = await _statisticsService.GetRevenueByStudentGroup(period);
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

        [HttpGet("service-type")]
        public async Task<IActionResult> GetRevenueByServiceType([FromQuery] PeriodRequestDTO request)
        {
            try
            {
                // Kiểm tra period hợp lệ
                string period = request.Period?.ToLower() ?? "tháng";
                if (!new[] { "tuần", "tháng", "năm" }.Contains(period))
                {
                    return BadRequest(new ResponseDTO<object>(400, "Period không hợp lệ, phải là 'tuần', 'tháng', hoặc 'năm'!", null));
                }

                var result = await _statisticsService.GetRevenueByServiceType(period);
                return Ok(new ResponseDTO<ServiceTypeRevenueDTO>(200, "Lấy thành công doanh số đến từ loại dịch vụ!", result));
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

        [HttpGet("room-performance/time")]
        public async Task<IActionResult> GetRoomPerformanceByTime([FromQuery] PeriodRequestDTO request)
        {
            try
            {
                // Kiểm tra period hợp lệ
                string period = request.Period?.ToLower() ?? "tháng";
                if (!new[] { "tuần", "tháng", "năm" }.Contains(period))
                {
                    return BadRequest(new ResponseDTO<object>(400, "Period không hợp lệ, phải là 'tuần', 'tháng', hoặc 'năm'!", null));
                }

                var result = await _statisticsService.GetRoomPerformanceByTime(period);
                return Ok(new ResponseDTO<List<RoomPerformanceDTO>>(200, "Lấy thành công hiệu suất phòng theo thời gian!", result));
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

        [HttpGet("room-performance/service-time")]
        public async Task<IActionResult> GetRoomPerformanceByServiceTime([FromQuery] PeriodRequestDTO request)
        {
            try
            {
                // Kiểm tra period hợp lệ
                string period = request.Period?.ToLower() ?? "tháng";
                if (!new[] { "tuần", "tháng", "năm" }.Contains(period))
                {
                    return BadRequest(new ResponseDTO<object>(400, "Period không hợp lệ, phải là 'tuần', 'tháng', hoặc 'năm'!", null));
                }

                var result = await _statisticsService.GetRoomPerformanceByServiceTime(period);
                return Ok(new ResponseDTO<List<RoomServicePerformanceDTO>>(200, "Lấy thành công hiệu suất phòng theo nhóm dịch vụ theo thời gian!", result));
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