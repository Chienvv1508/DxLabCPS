using AutoMapper;
using DxLabCoworkingSpace.Service.Sevices;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DxLabCoworkingSpace.Core.DTOs;
using OfficeOpenXml;

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

        // Add Facility For Excel File

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
                return Conflict(new { Message = ex.Message }); // Trùng BatchNumber
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
