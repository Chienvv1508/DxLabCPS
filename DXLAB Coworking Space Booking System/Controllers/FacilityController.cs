using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient.DataClassification;
using Microsoft.AspNetCore.Authorization;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/facility")]
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
        [HttpPost("importexcel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddFacilityFromExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Không có file nào được tải lên!", null));
                }
                if (!file.FileName.EndsWith(".xlsx"))
                {
                    return BadRequest(new ResponseDTO<object>(400, "Chỉ hỗ trợ file Excel (.xlsx)!", null));
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

                            if (string.IsNullOrEmpty(expiredTimeText))
                            {
                                return BadRequest(new ResponseDTO<object>(400, "ExpiredTime không được để trống!", null));
                            }
                            if (string.IsNullOrEmpty(importDateText))
                            {
                                return BadRequest(new ResponseDTO<object>(400, "ImportDate không được để trống!", null));
                            }

                            if (!DateTime.TryParseExact(expiredTimeText, "yyyy/MM/dd",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expired))
                            {
                                return BadRequest(new ResponseDTO<object>(400, "ExpiredTime phải có định dạng yyyy/MM/dd!", null));
                            }

                            if (!DateTime.TryParseExact(importDateText, "yyyy/MM/dd",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime import))
                            {
                                return BadRequest(new ResponseDTO<object>(400, "ImportDate phải có định dạng yyyy/MM/dd!", null));
                            }

                            if (!decimal.TryParse(worksheet.Cells[row, 3].Value?.ToString(), out decimal cost))
                            {
                                return BadRequest(new ResponseDTO<object>(400, "Cost không hợp lệ!", null));
                            }

                            if (!int.TryParse(worksheet.Cells[row, 5].Value?.ToString(), out int quantity))
                            {
                                return BadRequest(new ResponseDTO<object>(400, "Quantity không hợp lệ!", null));
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
                var facilityDtos = _mapper.Map<IEnumerable<FacilitiesDTO>>(facilities);
                return Created("", new ResponseDTO<IEnumerable<FacilitiesDTO>>(200, $"{facilities.Count} facility đã được thêm thành công!", facilityDtos));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseDTO<object>(400, ex.Message, null));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ResponseDTO<object>(409, ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi xử lý file Excel: {ex.Message}", null));
            }
        }
       
        // Add New Facility
        [HttpPost("createfacility")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateFacility([FromBody] FacilitiesDTO facilityDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ!", ModelState));
                }

                var facility = _mapper.Map<Facility>(facilityDto);
                await _facilityService.Add(facility);
                var resultDto = _mapper.Map<FacilitiesDTO>(facility);
                return Created("", new ResponseDTO<FacilitiesDTO>(200, "Facility đã được thêm thành công!", resultDto));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ResponseDTO<object>(409, ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi thêm facility: {ex.Message}", null));
            }
        }

        // Get All Account
        [HttpGet]
        public async Task<IActionResult> GetAllFacilities()
        {
            try
            {
                var facilities = await _facilityService.GetAll();
                var facilityDtos = _mapper.Map<IEnumerable<FacilitiesDTO>>(facilities);
                return Ok(new ResponseDTO<IEnumerable<FacilitiesDTO>>(200, "Danh sách facility được lấy thành công!", facilityDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi lấy danh sách facility: {ex.Message}", null));
            }
        }

        //Get An Facility
        [HttpGet("{facilityid}/{batchnumber}")]
        public async Task<IActionResult> GetAnFacility(int facilityid, string batchnumber)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ!", ModelState));
                }

                var facility = await _facilityService.Get(f => f.FacilityId == facilityid && f.BatchNumber == batchnumber);
                if (facility == null)
                {
                    return NotFound(new ResponseDTO<object>(404, "Không tìm thấy facility với FacilityId và BatchNumber đã cung cấp!", null));
                }

                var facilityDto = _mapper.Map<FacilitiesDTO>(facility);
                return Ok(new ResponseDTO<FacilitiesDTO>(200, "Facility được lấy thành công!", facilityDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi lấy facility: {ex.Message}", null));
            }
        }

        //Update Facility

    }
}
