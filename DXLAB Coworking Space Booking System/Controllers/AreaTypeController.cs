using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
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
            var existedAreaType = await _areaTypeService.Get(x => x.AreaTypeName == areTypeDto.AreaTypeName);
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
            try
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
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(01, "Lỗi khi lấy tất cả loại khu vực", null);
                return StatusCode(500, response);
            }


        }

        [HttpGet]
        public async Task<IActionResult> GetAllAreaType()
        {
            try
            {
                var areaTypes = await _areaTypeService.GetAll();
                var areaTypesDTO = _mapper.Map<IEnumerable<AreaTypeDTO>>(areaTypes);
                var response = new ResponseDTO<object>(200, "Lấy thành công", areaTypesDTO);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(01, "Lỗi khi lấy tất cả loại khu vực", null);
                return StatusCode(500, response);
            }

        }

        [HttpGet("areatypeforselection")]
        public async Task<IActionResult> GetAreaTypeForSelection()
        {
            try
            {
                var areaTypes = await _areaTypeService.GetAreaTypeForAddRoom();
                var areaTypesDTO = _mapper.Map<IEnumerable<AreaAddDTO>>(areaTypes);
                var response = new ResponseDTO<object>(200, "Lấy thành công", areaTypesDTO);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(01, "Lỗi khi lấy tất cả loại khu vực", null);
                return StatusCode(500, response);
            }

        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchRoom(int id, [FromBody] JsonPatchDocument<AreaType> patchDoc)
        {
            try
            {
                if (patchDoc == null)
                {
                    var response = new ResponseDTO<object>(400, "Bạn chưa truyền dữ liệu vào", null);
                    return BadRequest(response);
                }
                var allowedPaths = new HashSet<string>
            {
                       "areaTypeName",
                        "areaDescription",
                        "price",
                        "images",
                        "isDeleted"

            };
                var areaTypeNameOp = patchDoc.Operations.FirstOrDefault(op => op.path.Equals("areaTypeName", StringComparison.OrdinalIgnoreCase));
                if (areaTypeNameOp != null)
                {
                    var existedAreaType = await _areaTypeService.Get(x => x.AreaTypeName == areaTypeNameOp.value.ToString());
                    if (existedAreaType != null)
                    {
                        var response = new ResponseDTO<object>(400, $"Tên loại phòng {areaTypeNameOp} đã tồn tại. Vui lòng nhập tên loại phòng khác!", null);
                        return BadRequest(response);
                    }
                }


                foreach (var operation in patchDoc.Operations)
                {
                    if (!allowedPaths.Contains(operation.path))
                    {
                        var response1 = new ResponseDTO<object>(400, $"Không thể cập nhật trường: {operation.path}", null);
                        return BadRequest(response1);
                    }
                }



                var areaTypeFromDb = await _areaTypeService.Get(x => x.AreaTypeId == id);
                if (areaTypeFromDb == null)
                {
                    var response2 = new ResponseDTO<object>(404, "Không tìm thấy phòng. Vui lòng nhập lại mã loại phòng!", null);
                    return NotFound(response2);
                }

                patchDoc.ApplyTo(areaTypeFromDb, ModelState);


                if (!ModelState.IsValid)
                {
                    var allErrors = ModelState
                    .SelectMany(ms => ms.Value.Errors
                    .Select(err => $"{ms.Key}: {err.ErrorMessage}"))
                    .ToList();
                    string errorString = string.Join(" | ", allErrors);
                    var response = new ResponseDTO<object>(400, errorString, null);
                    return BadRequest(response);
                }

                var areaTypeDTO = _mapper.Map<AreaTypeDTO>(areaTypeFromDb);

                bool isValid = TryValidateModel(areaTypeDTO);
                if (!isValid)
                {
                    var allErrors = ModelState
                    .SelectMany(ms => ms.Value.Errors
                    .Select(err => $"{ms.Key}: {err.ErrorMessage}"))
                    .ToList();

                    string errorString = string.Join(" | ", allErrors);
                    var response = new ResponseDTO<object>(404, errorString, null);
                    return BadRequest(response);
                }
                await _areaTypeService.Update(areaTypeFromDb);
                var response3 = new ResponseDTO<object>(200, "Cập nhập thành công!", null);
                return Ok(response3);
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(01, "Lỗi khi cập nhập dữ liệu!", null);
                return StatusCode(500, response);

            }


        }

    }
}
