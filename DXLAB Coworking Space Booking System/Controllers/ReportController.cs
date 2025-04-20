using AutoMapper;
using DXLAB_Coworking_Space_Booking_System.Hubs;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/report")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IBookingDetailService _bookDetailService;
        private readonly IUserService _userService;
        private readonly IAreaService _areaService;
        private readonly IMapper _mapper;
        private readonly IHubContext<ReportHub> _hubContext;
        private readonly DxLabSystemContext _context;

        public ReportController(
            IReportService reportService,
            IBookingDetailService bookDetailService,
            IUserService userService,
            IAreaService areaService,
            IMapper mapper,
            IHubContext<ReportHub> hubContext,
            DxLabSystemContext context)
        {
            _reportService = reportService;
            _bookDetailService = bookDetailService;
            _userService = userService;
            _areaService = areaService;
            _mapper = mapper;
            _hubContext = hubContext;
            _context = context;
        }

        // Phương thức tiện ích để ánh xạ Report sang ReportResponseDTO
        private async Task<ReportResponseDTO> MapToReportResponseDTO(Report report)
        {
            var response = new ReportResponseDTO
            {
                ReportId = report.ReportId,
                BookingDetailId = report.BookingDetailId,
                CreatedDate = report.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                StaffName = report.User?.FullName ?? "N/A"
            };

            if (report.BookingDetail != null)
            {
                string positionDisplay = null;
                string areaName = null;
                string areaTypeName = null;
                string roomName = null;
                int? facilityId = null;
                string batchNumber = null;
                string facilityTitle = null;

                if (report.BookingDetail.Area?.AreaId != null)
                {
                    areaName = report.BookingDetail.Area.AreaName;
                    areaTypeName = report.BookingDetail.Area.AreaType?.AreaTypeName ?? "N/A";
                    roomName = report.BookingDetail.Area.Room?.RoomName ?? "N/A";
                    positionDisplay = report.BookingDetail.Position != null
                        ? report.BookingDetail.Position.PositionNumber.ToString()
                        : report.BookingDetail.Area.AreaType?.AreaTypeName ?? "N/A";

                    Console.WriteLine($"[ReportId: {report.ReportId}] Fetching UsingFacilities for AreaId: {report.BookingDetail.Area.AreaId}");
                    try
                    {
                        var areaWithFacilities = await _context.Areas
                            .Where(a => a.AreaId == report.BookingDetail.Area.AreaId)
                            .Include(a => a.UsingFacilities)
                            .ThenInclude(uf => uf.Facility)
                            .FirstOrDefaultAsync();

                        if (areaWithFacilities?.UsingFacilities?.Any() == true)
                        {
                            var uf = areaWithFacilities.UsingFacilities.OrderByDescending(uf => uf.ImportDate).FirstOrDefault();
                            Console.WriteLine($"[ReportId: {report.ReportId}] Found UsingFacility: FacilityId={uf?.FacilityId}, BatchNumber={uf?.BatchNumber}, FacilityTitle={uf?.Facility?.FacilityTitle}");
                            facilityId = uf?.FacilityId;
                            batchNumber = uf?.BatchNumber ?? uf?.Facility?.BatchNumber ?? "N/A";
                            facilityTitle = uf?.Facility?.FacilityTitle ?? "N/A";
                        }
                        else
                        {
                            Console.WriteLine($"[ReportId: {report.ReportId}] No UsingFacilities found for AreaId: {report.BookingDetail.Area.AreaId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ReportId: {report.ReportId}] Error fetching UsingFacilities: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        // Tiếp tục với giá trị mặc định nếu lỗi
                    }
                }
                else if (report.BookingDetail.Position != null)
                {
                    positionDisplay = report.BookingDetail.Position.PositionNumber.ToString();
                    Console.WriteLine($"[ReportId: {report.ReportId}] Fetching UsingFacilities for AreaId: {report.BookingDetail.Position.AreaId}");
                    try
                    {
                        var area = await _context.Areas
                            .Where(a => a.AreaId == report.BookingDetail.Position.AreaId)
                            .Include(a => a.Room)
                            .Include(a => a.AreaType)
                            .Include(a => a.UsingFacilities)
                            .ThenInclude(uf => uf.Facility)
                            .FirstOrDefaultAsync();

                        if (area != null)
                        {
                            areaName = area.AreaName;
                            areaTypeName = area.AreaType?.AreaTypeName ?? "N/A";
                            roomName = area.Room?.RoomName ?? "N/A";
                            if (area.UsingFacilities?.Any() == true)
                            {
                                var uf = area.UsingFacilities.OrderByDescending(uf => uf.ImportDate).FirstOrDefault();
                                Console.WriteLine($"[ReportId: {report.ReportId}] Found UsingFacility: FacilityId={uf?.FacilityId}, BatchNumber={uf?.BatchNumber}, FacilityTitle={uf?.Facility?.FacilityTitle}");
                                facilityId = uf?.FacilityId;
                                batchNumber = uf?.BatchNumber ?? uf?.Facility?.BatchNumber ?? "N/A";
                                facilityTitle = uf?.Facility?.FacilityTitle ?? "N/A";
                            }
                            else
                            {
                                Console.WriteLine($"[ReportId: {report.ReportId}] No UsingFacilities found for AreaId: {report.BookingDetail.Position.AreaId}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[ReportId: {report.ReportId}] No Area found for Position AreaId: {report.BookingDetail.Position.AreaId}");
                            areaName = "N/A";
                            areaTypeName = "N/A";
                            roomName = "N/A";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ReportId: {report.ReportId}] Error fetching UsingFacilities: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        // Tiếp tục với giá trị mặc định nếu lỗi
                    }
                }

                response.Position = positionDisplay ?? "N/A";
                response.AreaName = areaName ?? "N/A";
                response.AreaTypeName = areaTypeName ?? "N/A";
                response.RoomName = roomName ?? "N/A";
                response.FacilityId = facilityId;
                response.BatchNumber = batchNumber ?? "N/A";
                response.FacilityTitle = facilityTitle ?? "N/A";
            }
            else
            {
                response.Position = "N/A";
                response.AreaName = "N/A";
                response.AreaTypeName = "N/A";
                response.RoomName = "N/A";
                response.FacilityId = null;
                response.BatchNumber = "N/A";
                response.FacilityTitle = "N/A";
            }

            return response;
        }

        // 1. Tạo báo cáo cơ sở vật chất (POST /api/report)
        [HttpPost]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> CreateReport([FromBody] ReportRequestDTO request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ReportDescription))
                {
                    return BadRequest(new ResponseDTO<object>(400, "Mô tả báo cáo không được để trống!", null));
                }

                // Kiểm tra staffId
                var staffId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (staffId == 0)
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Không thể xác định Staff!", null));
                }

                // Kiểm tra User tồn tại
                var staff = await _userService.GetById(staffId);
                if (staff == null)
                {
                    return BadRequest(new ResponseDTO<object>(400, $"User với ID {staffId} không tồn tại!", null));
                }

                // Kiểm tra BookingDetailId
                BookingDetail? bookingDetail = null;
                if (request.BookingDetailId.HasValue)
                {
                    bookingDetail = await _bookDetailService.GetWithInclude(
                        bd => bd.BookingDetailId == request.BookingDetailId.Value,
                        bd => bd.Area,
                        bd => bd.Area.Room,
                        bd => bd.Area.AreaType,
                        bd => bd.Position
                    );
                    if (bookingDetail == null)
                    {
                        return NotFound(new ResponseDTO<object>(404, $"Không tìm thấy BookingDetail với ID {request.BookingDetailId}!", null));
                    }

                    // Kiểm tra xem BookingDetailId đã được sử dụng trong Reports chưa
                    bool existingReport = await _context.Reports.AnyAsync(r => r.BookingDetailId == request.BookingDetailId.Value);
                    if (existingReport)
                    {
                        return BadRequest(new ResponseDTO<object>(400, $"BookingDetail với ID {request.BookingDetailId} đã có báo cáo!", null));
                    }
                }

                var report = _mapper.Map<Report>(request);
                report.UserId = staffId;

                // Ghi log để kiểm tra dữ liệu trước khi lưu
                Console.WriteLine($"Creating Report: UserId={report.UserId}, BookingDetailId={report.BookingDetailId}, ReportDescription={report.ReportDescription}, CreatedDate={report.CreatedDate}");

                await _reportService.Add(report);

                // Sử dụng phương thức tiện ích để ánh xạ
                var responseData = await MapToReportResponseDTO(report);

                var reportData = new
                {
                    responseData.ReportId,
                    responseData.BookingDetailId,
                    responseData.FacilityId,
                    responseData.BatchNumber,
                    responseData.FacilityTitle,
                    responseData.Position,
                    responseData.AreaName,
                    responseData.AreaTypeName,
                    responseData.RoomName,
                    responseData.CreatedDate,
                    responseData.StaffName,
                    ReportDescription = request.ReportDescription
                };
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNewReport", reportData);

                return Ok(new ResponseDTO<ReportResponseDTO>(200, "Tạo báo cáo cơ sở vật chất thành công!", responseData));
            }
            catch (Exception ex)
            {
                // Trả về chi tiết InnerException
                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"Error in CreateReport: {errorMessage}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi tạo báo cáo cơ sở vật chất", errorMessage));
            }
        }

        // 2. Xem danh sách báo cáo của Staff hiện tại (GET /api/report/staffreport)
        [HttpGet("staffreport")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetStaffReports()
        {
            try
            {
                var staffId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (staffId == 0)
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Không thể xác định Staff!", null));
                }

                var allReports = await _reportService.GetAllWithInclude(
                    r => r.BookingDetail,
                    r => r.BookingDetail.Area,
                    r => r.BookingDetail.Area.Room,
                    r => r.BookingDetail.Area.AreaType,
                    r => r.BookingDetail.Position,
                    r => r.User
                );

                var reports = allReports.Where(r => r.UserId == staffId).ToList();

                if (!reports.Any())
                {
                    return Ok(new ResponseDTO<object>(200, "Bạn chưa tạo báo cáo nào!", null));
                }

                var responseData = new List<ReportResponseDTO>();
                foreach (var report in reports)
                {
                    var response = await MapToReportResponseDTO(report);
                    responseData.Add(response);
                }

                return Ok(new ResponseDTO<List<ReportResponseDTO>>(200, "Lấy danh sách báo cáo của bạn thành công!", responseData));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"Error in GetStaffReports: {errorMessage}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi lấy danh sách báo cáo", errorMessage));
            }
        }

        // 3. Xem tất cả danh sách báo cáo của mọi Staff (GET /api/report/getallreport)
        [HttpGet("getallreport")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllReports()
        {
            try
            {
                var reports = await _reportService.GetAllWithInclude(
                    r => r.BookingDetail,
                    r => r.BookingDetail.Area,
                    r => r.BookingDetail.Area.Room,
                    r => r.BookingDetail.Area.AreaType,
                    r => r.BookingDetail.Position,
                    r => r.User
                );

                if (!reports.Any())
                {
                    return Ok(new ResponseDTO<object>(200, "Không có báo cáo nào!", null));
                }

                var responseData = new List<ReportResponseDTO>();
                foreach (var report in reports)
                {
                    var response = await MapToReportResponseDTO(report);
                    responseData.Add(response);
                }

                return Ok(new ResponseDTO<List<ReportResponseDTO>>(200, "Lấy danh sách tất cả báo cáo thành công!", responseData));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"Error in GetAllReports: {errorMessage}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi lấy danh sách báo cáo", errorMessage));
            }
        }

        // 4. Xem chi tiết báo cáo theo ReportId (GET /api/report/{reportId})
        [HttpGet("{reportId}")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> GetReportById(int reportId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Không thể xác định người dùng!", null));
                }

                var report = await _reportService.GetWithInclude(
                    r => r.ReportId == reportId,
                    r => r.BookingDetail,
                    r => r.BookingDetail.Area,
                    r => r.BookingDetail.Area.Room,
                    r => r.BookingDetail.Area.AreaType,
                    r => r.BookingDetail.Position,
                    r => r.User
                );

                if (report == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Không tìm thấy báo cáo với ID {reportId}!", null));
                }

                var isStaff = User.IsInRole("Staff");
                if (isStaff && report.UserId != userId)
                {
                    return Forbid();
                }

                var responseData = await MapToReportResponseDTO(report);

                return Ok(new ResponseDTO<ReportResponseDTO>(200, "Lấy chi tiết báo cáo thành công!", responseData));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"Error in GetReportById: {errorMessage}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi lấy chi tiết báo cáo", errorMessage));
            }
        }
    }
}