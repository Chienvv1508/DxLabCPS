using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System
{
    [Route("api/areatypecategory")]
    [ApiController]
    public class AreaTypeCategoryController : ControllerBase
    {
        private readonly IAreaTypeCategoryService _areaTypeCategoryService;
        private readonly IMapper _mapper;

        public AreaTypeCategoryController(IAreaTypeCategoryService areaTypeCategoryService, IMapper mapper)
        {
            _areaTypeCategoryService = areaTypeCategoryService;
            _mapper = mapper;
        }

        [HttpPost("newareatypecategory")]
        public async Task<IActionResult> CreateAreaTypeCategory([FromForm] AreaTypeCategoryDTO areaTypeCategoryDTO)
        {
            var areaTypeCategory = _mapper.Map<AreaTypeCategory>(areaTypeCategoryDTO);
            await _areaTypeCategoryService.Add(areaTypeCategory);
            return Ok();
        }

        [HttpGet("allAreaTypeCategory")]
        public async Task<IActionResult> GetAllAreaTyepCategory()
        {
            try
            {
                var areaTypeCategorys = await _areaTypeCategoryService.GetAllWithInclude(x => x.Images);
                var areaTypeCategoryDTOS = _mapper.Map<IEnumerable<AreaTypeCategoryDTO>>(areaTypeCategorys);
                var response = new ResponseDTO<object>(200, "Danh sách các loại: ", areaTypeCategoryDTOS);
                return Ok(response);
            }
            catch(Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, "Lỗi database!", null));
            }
            
        }
        [HttpPatch()]
        public async Task<IActionResult> PatchAreaTypeCategory(int id, [FromBody] JsonPatchDocument<AreaTypeCategory> patchDoc)
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
                       "title",
                        "categoryDescription",
                        "status",
                        "images"

                };
                var areaTypeCategoryTitleOp = patchDoc.Operations.FirstOrDefault(op => op.path.Equals("title", StringComparison.OrdinalIgnoreCase));
                if (areaTypeCategoryTitleOp != null)
                {
                    var existedAreaTypeCategory = await _areaTypeCategoryService.Get(x => x.Title == areaTypeCategoryTitleOp.value.ToString() && x.Status == 1);
                    if (existedAreaTypeCategory != null)
                    {
                        var response = new ResponseDTO<object>(400, $"Tên loại {areaTypeCategoryTitleOp.value.ToString()} đã tồn tại. Vui lòng nhập tên loại phòng khác!", null);
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

                var areaTypeCateFromDb = await _areaTypeCategoryService.Get(x => x.CategoryId == id);
                if (areaTypeCateFromDb == null)
                {
                    var response2 = new ResponseDTO<object>(404, "Không tìm loại!", null);
                    return NotFound(response2);
                }

                patchDoc.ApplyTo(areaTypeCateFromDb, ModelState);

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

                var areTypeCates = _mapper.Map<AreaTypeCategoryDTO>(areaTypeCateFromDb);

                bool isValid = TryValidateModel(areTypeCates);
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
                await _areaTypeCategoryService.Update(areaTypeCateFromDb);
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
