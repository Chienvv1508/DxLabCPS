using AutoMapper;
using DXLAB_Coworking_Space_Booking_System.Hubs;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

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

        public ReportController(IReportService reportService, IBookingDetailService bookingDetailService, IUserService userService, IAreaService areaService,IMapper mapper, IHubContext<ReportHub> hubContext)
        {
            _reportService = reportService;
            _bookDetailService = bookingDetailService;
            _userService = userService;
            _areaService = areaService;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        // Tạo báo cáo cơ sở vật chất
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
                }

                var staffId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (staffId == 0)
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Không thể xác định Staff!", null));
                }

                var report = _mapper.Map<Report>(request);
                report.UserId = staffId;

                await _reportService.Add(report);

                // Chuẩn bị dữ liệu trả về thủ công
                var responseData = new ReportResponseDTO
                {
                    ReportId = report.ReportId,
                    BookingDetailId = report.BookingDetailId,
                    CreatedDate = report.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ss")
                };

                if (bookingDetail != null)
                {
                    string positionDisplay = null;
                    string areaName = null;
                    string areaTypeName = null;
                    string roomName = null;

                    if (bookingDetail.Area?.AreaId != null)
                    {
                        areaName = bookingDetail.Area.AreaName;
                        areaTypeName = bookingDetail.Area.AreaType?.AreaTypeName ?? "N/A";
                        roomName = bookingDetail.Area.Room?.RoomName ?? "N/A";
                        // Nếu Position null, dùng AreaTypeName thay vì "N/A"
                        positionDisplay = bookingDetail.Position != null
                            ? bookingDetail.Position.PositionNumber.ToString()
                            : bookingDetail.Area.AreaType?.AreaTypeName ?? "N/A";
                    }
                    else if (bookingDetail.Position != null)
                    {
                        positionDisplay = bookingDetail.Position.PositionNumber.ToString();
                        var area = await _areaService.GetWithInclude(
                            a => a.AreaId == bookingDetail.Position.AreaId,
                            a => a.Room,
                            a => a.AreaType
                        );
                        if (area != null)
                        {
                            areaName = area.AreaName;
                            areaTypeName = area.AreaType?.AreaTypeName ?? "N/A";
                            roomName = area.Room?.RoomName ?? "N/A";
                        }
                        else
                        {
                            areaName = "N/A";
                            areaTypeName = "N/A";
                            roomName = "N/A";
                        }
                    }

                    responseData.Position = positionDisplay ?? "N/A";
                    responseData.AreaName = areaName ?? "N/A";
                    responseData.AreaTypeName = areaTypeName ?? "N/A";
                    responseData.RoomName = roomName ?? "N/A";
                }
                else
                {
                    responseData.Position = "N/A";
                    responseData.AreaName = "N/A";
                    responseData.AreaTypeName = "N/A";
                    responseData.RoomName = "N/A";
                }

                var staff = await _userService.GetById(staffId);
                responseData.StaffName = staff?.FullName ?? "N/A";

                // Gửi thông báo real-time tới Admin
                var reportData = new
                {
                    responseData.ReportId,
                    responseData.BookingDetailId,
                    responseData.CreatedDate,
                    responseData.StaffName,
                    responseData.Position,
                    responseData.AreaName,
                    responseData.AreaTypeName,
                    responseData.RoomName,
                    ReportDescription = request.ReportDescription // Thêm mô tả báo cáo
                };
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNewReport", reportData);

                return Ok(new ResponseDTO<ReportResponseDTO>(200, "Tạo báo cáo cơ sở vật chất thành công!", responseData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi tạo báo cáo cơ sở vật chất", ex.Message));
            }
        }

        //Xem danh sách báo cáo của Staff tạo ra
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

                        if (report.BookingDetail.Area?.AreaId != null)
                        {
                            areaName = report.BookingDetail.Area.AreaName;
                            areaTypeName = report.BookingDetail.Area.AreaType?.AreaTypeName ?? "N/A";
                            roomName = report.BookingDetail.Area.Room?.RoomName ?? "N/A";
                            positionDisplay = report.BookingDetail.Position != null
                                ? report.BookingDetail.Position.PositionNumber.ToString()
                                : report.BookingDetail.Area.AreaType?.AreaTypeName ?? "N/A";
                        }
                        else if (report.BookingDetail.Position != null)
                        {
                            positionDisplay = report.BookingDetail.Position.PositionNumber.ToString();
                            var area = await _areaService.GetWithInclude(
                                a => a.AreaId == report.BookingDetail.Position.AreaId,
                                a => a.Room,
                                a => a.AreaType
                            );
                            if (area != null)
                            {
                                areaName = area.AreaName;
                                areaTypeName = area.AreaType?.AreaTypeName ?? "N/A";
                                roomName = area.Room?.RoomName ?? "N/A";
                            }
                            else
                            {
                                areaName = "N/A";
                                areaTypeName = "N/A";
                                roomName = "N/A";
                            }
                        }

                        response.Position = positionDisplay ?? "N/A";
                        response.AreaName = areaName ?? "N/A";
                        response.AreaTypeName = areaTypeName ?? "N/A";
                        response.RoomName = roomName ?? "N/A";
                    }
                    else
                    {
                        response.Position = "N/A";
                        response.AreaName = "N/A";
                        response.AreaTypeName = "N/A";
                        response.RoomName = "N/A";
                    }

                    responseData.Add(response);
                }

                return Ok(new ResponseDTO<List<ReportResponseDTO>>(200, "Lấy danh sách báo cáo của bạn thành công!", responseData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi lấy danh sách báo cáo", ex.Message));
            }
        }

        //Xem tất cả danh sách báo cáo của mọi Staff
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

                        if (report.BookingDetail.Area?.AreaId != null)
                        {
                            areaName = report.BookingDetail.Area.AreaName;
                            areaTypeName = report.BookingDetail.Area.AreaType?.AreaTypeName ?? "N/A";
                            roomName = report.BookingDetail.Area.Room?.RoomName ?? "N/A";
                            positionDisplay = report.BookingDetail.Position != null
                                ? report.BookingDetail.Position.PositionNumber.ToString()
                                : report.BookingDetail.Area.AreaType?.AreaTypeName ?? "N/A";
                        }
                        else if (report.BookingDetail.Position != null)
                        {
                            positionDisplay = report.BookingDetail.Position.PositionNumber.ToString();
                            var area = await _areaService.GetWithInclude(
                                a => a.AreaId == report.BookingDetail.Position.AreaId,
                                a => a.Room,
                                a => a.AreaType
                            );
                            if (area != null)
                            {
                                areaName = area.AreaName;
                                areaTypeName = area.AreaType?.AreaTypeName ?? "N/A";
                                roomName = area.Room?.RoomName ?? "N/A";
                            }
                            else
                            {
                                areaName = "N/A";
                                areaTypeName = "N/A";
                                roomName = "N/A";
                            }
                        }

                        response.Position = positionDisplay ?? "N/A";
                        response.AreaName = areaName ?? "N/A";
                        response.AreaTypeName = areaTypeName ?? "N/A";
                        response.RoomName = roomName ?? "N/A";
                    }
                    else
                    {
                        response.Position = "N/A";
                        response.AreaName = "N/A";
                        response.AreaTypeName = "N/A";
                        response.RoomName = "N/A";
                    }

                    responseData.Add(response);
                }

                return Ok(new ResponseDTO<List<ReportResponseDTO>>(200, "Lấy danh sách tất cả báo cáo thành công!", responseData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi lấy danh sách báo cáo", ex.Message));
            }
        }

        // Xem chi tiết theo reportId
        [HttpGet("{reportId}")]
        [Authorize(Roles = "Staff, Admin")]
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
                    return Forbid(); // 403: Staff không có quyền xem báo cáo của người khác
                }

                var responseData = new ReportResponseDTO
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

                    if (report.BookingDetail.Area?.AreaId != null)
                    {
                        areaName = report.BookingDetail.Area.AreaName;
                        areaTypeName = report.BookingDetail.Area.AreaType?.AreaTypeName ?? "N/A";
                        roomName = report.BookingDetail.Area.Room?.RoomName ?? "N/A";
                        positionDisplay = report.BookingDetail.Position != null
                            ? report.BookingDetail.Position.PositionNumber.ToString()
                            : report.BookingDetail.Area.AreaType?.AreaTypeName ?? "N/A";
                    }
                    else if (report.BookingDetail.Position != null)
                    {
                        positionDisplay = report.BookingDetail.Position.PositionNumber.ToString();
                        areaName = "N/A"; // Nếu không có Area trực tiếp, cần truy vấn thêm
                        areaTypeName = "N/A";
                        roomName = "N/A";
                    }

                    responseData.Position = positionDisplay ?? "N/A";
                    responseData.AreaName = areaName ?? "N/A";
                    responseData.AreaTypeName = areaTypeName ?? "N/A";
                    responseData.RoomName = roomName ?? "N/A";
                }
                else
                {
                    responseData.Position = "N/A";
                    responseData.AreaName = "N/A";
                    responseData.AreaTypeName = "N/A";
                    responseData.RoomName = "N/A";
                }

                return Ok(new ResponseDTO<ReportResponseDTO>(200, "Lấy chi tiết báo cáo thành công!", responseData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi lấy chi tiết báo cáo", ex.Message));
            }
        }
    }
}
