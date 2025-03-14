using AutoMapper;
using DxLabCoworkingSpace.Service.Sevices;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DxLabCoworkingSpace.Core.DTOs;
using OfficeOpenXml;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacilityController : ControllerBase
    {
        private readonly IFacilityService _facilityService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public FacilityController(IFacilityService facilityService, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _facilityService = facilityService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // Add Facility From Excel File
        [HttpPost("AddFacilityFromExcel")]
        public async Task<IActionResult> AddFacilityFromExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Message = "Không có file nào được tải lên!" });
                }
                if (!file.FileName.EndsWith(".xlsx"))
                {
                    return BadRequest(new { Message = "Chỉ hỗ trợ file Excel (.xlsx)!" });
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var facilities = new List<Facility>();
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            string expiredTimeText = worksheet.Cells[row, 4].Text?.Trim() ?? "";
                            string importDateText = worksheet.Cells[row, 6].Text?.Trim() ?? "";

                            // Debug: In giá trị thô để kiểm tra
                            if (string.IsNullOrEmpty(expiredTimeText))
                            {
                                return BadRequest(new { Message = $"ExpiredTime trống tại dòng {row}!" });
                            }
                            if (string.IsNullOrEmpty(importDateText))
                            {
                                return BadRequest(new { Message = $"ImportDate trống tại dòng {row}!" });
                            }

                            // Parse ExpiredTime
                            if (!DateTime.TryParseExact(expiredTimeText, "yyyy/MM/dd",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expired))
                            {
                                return BadRequest(new { Message = $"ExpiredTime không hợp lệ tại dòng {row}, giá trị thô: '{expiredTimeText}', phải có định dạng yyyy/MM/dd (ví dụ: 2026/04/30)!" });
                            }

                            // Parse ImportDate
                            if (!DateTime.TryParseExact(importDateText, "yyyy/MM/dd",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime import))
                            {
                                return BadRequest(new { Message = $"ImportDate không hợp lệ tại dòng {row}, giá trị thô: '{importDateText}', phải có định dạng yyyy/MM/dd (ví dụ: 2025/04/30)!" });
                            }

                            // Parse Cost và Quantity
                            if (!decimal.TryParse(worksheet.Cells[row, 3].Value?.ToString(), out decimal cost))
                            {
                                return BadRequest(new { Message = $"Cost không hợp lệ tại dòng {row}, phải là số!" });
                            }

                            if (!int.TryParse(worksheet.Cells[row, 5].Value?.ToString(), out int quantity))
                            {
                                return BadRequest(new { Message = $"Quantity không hợp lệ tại dòng {row}, phải là số nguyên!" });
                            }

                            facilities.Add(new Facility
                            {
                                BatchNumber = worksheet.Cells[row, 1].Value?.ToString() ?? "",
                                FacilityDescription = worksheet.Cells[row, 2].Value?.ToString(),
                                Cost = cost,
                                ExpiredTime = expired,
                                Quantity = quantity,
                                ImportDate = import
                            });
                        }
                    }
                }

                await _facilityService.AddFacilityFromExcel(facilities);
                var facilityDtos = _mapper.Map<List<FacilitiesDTO>>(facilities);
                return Created("", new
                {
                    Message = $"{facilities.Count} facility đã được thêm thành công!",
                    Facilities = facilityDtos
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi xử lý file Excel: {ex.Message}" });
            }
        }
        // Add New Facility
        [HttpPost("AddNewFacility")]
        public async Task<IActionResult> CreateFacility([FromBody] FacilitiesDTO facilityDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Dữ liệu không hợp lệ!", Errors = ModelState });
                }

                var facility = _mapper.Map<Facility>(facilityDto);
                await _facilityService.Add(facility);
                var resultDto = _mapper.Map<FacilitiesDTO>(facility);
                return Created("", new
                {
                    Message = "Facility đã được thêm thành công!",
                    Facility = resultDto
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message }); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi khi thêm facility: {ex.Message}" });
            }
        }

        // Get All Account
        [HttpGet("GetAllFacilities")]
        public async Task<IActionResult> GetAllFacilities()
        {
            try
            {
                var facilities = await _facilityService.GetAll();
                var facilityDtos = _mapper.Map<IEnumerable<FacilitiesDTO>>(facilities);
                return Ok(new
                {
                    Message = "Danh sách facility được lấy thành công!",
                    Facilities = facilityDtos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi khi lấy danh sách facility: {ex.Message}" });
            }
        }
    }
}
