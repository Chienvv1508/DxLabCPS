using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreaTypeController : ControllerBase
    {
        private readonly IAreaTypeService _areaTypeService;
        private readonly IMapper _mapper;

        public AreaTypeController(IAreaTypeService areaTypeService, IMapper mapper)
        {
            _areaTypeService = areaTypeService;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAreaType([FromBody] AreaTypeDTO areTypeDto)
        {
            var existedAreaType =await _areaTypeService.Get(x => x.AreaName == areTypeDto.AreaName);
            if (existedAreaType != null)
            {

                var response = new ResponseDTO<object>(400, "Tên phòng đã tồn tại. Vui lòng nhập tên phòng khác", null);
                return BadRequest(response);
            }

            try
            {
                var areaType = _mapper.Map<AreaType>(areTypeDto);
                await _areaTypeService.Add(areaType);
                areTypeDto = _mapper.Map<AreaTypeDTO>(areaType);

                var response = new ResponseDTO<AreaTypeDTO>(201, "Tạo loại khu vực thành công", areTypeDto);
                return CreatedAtAction(nameof(GetAreTypeById), new { id = areaType.AreaTypeId }, response);
            }
            catch (DbUpdateException ex)
            {
                var response = new ResponseDTO<object>(500, "Lỗi cập nhật cơ sở dữ liệu.", ex.Message);
                return StatusCode(500, response);
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(500, "Đã xảy ra lỗi khi tạo phòng.", ex.Message);
                return StatusCode(500, response);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAreTypeById(int id)
        {
            var areaType = await _areaTypeService.Get(x => x.AreaTypeId == id);
            if (areaType == null)
            {
                var responseNotFound = new ResponseDTO<object>(404, "Mã AreaType không tồn tại", null);
                return NotFound(responseNotFound);
            }
            var areaTypeDTO = _mapper.Map<AreaTypeDTO>(areaType);
            var response = new ResponseDTO<object>(200, "Lấy thành công", areaTypeDTO);
            return Ok(response);

        }

        [HttpGet]
        public async Task<IActionResult> GetAllAreaType()
        {
            var areaTypes = await _areaTypeService.GetAll();
            var areaTypesDTO = _mapper.Map<IEnumerable<AreaTypeDTO>>(areaTypes);
            var response = new ResponseDTO<object>(200, "Lấy thành công", areaTypesDTO);
            return Ok(response);
        }

    }
}