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
        [HttpPatch]
        public async Task<IActionResult> PatchAreaTypeCategory(int id, [FromForm] AreaTypeCategoryDTO updatedData)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (updatedData == null)
                {
                    var response = new ResponseDTO<object>(400, "Bạn chưa truyền dữ liệu vào", null);
                    return BadRequest(response);
                }

                // Lấy entity từ database
                var areaTypeCateFromDb = await _areaTypeCategoryService.Get(x => x.CategoryId == id);
                if (areaTypeCateFromDb == null)
                {
                    var response = new ResponseDTO<object>(404, "Không tìm thấy loại khu vực!", null);
                    return NotFound(response);
                }

                // Kiểm tra các trường được phép cập nhật
                var allowedFields = new HashSet<string>
                {
                    "Title",
                    "CategoryDescription",
                    "Status",
                    "Images"
                };

                // Kiểm tra title trùng lặp nếu có thay đổi
                if (!string.IsNullOrEmpty(updatedData.Title) && updatedData.Title != areaTypeCateFromDb.Title)
                {
                    var existedAreaTypeCategory = await _areaTypeCategoryService.Get(x => x.Title == updatedData.Title && x.Status == 1);
                    if (existedAreaTypeCategory != null)
                    {
                        var response = new ResponseDTO<object>(400, $"Tên loại {updatedData.Title} đã tồn tại. Vui lòng nhập tên loại khác!", null);
                        return BadRequest(response);
                    }
                }

                // Cập nhật các trường từ DTO sang entity (chỉ cập nhật nếu có giá trị mới)
                if (!string.IsNullOrEmpty(updatedData.Title))
                {
                    areaTypeCateFromDb.Title = updatedData.Title;
                }
                if (!string.IsNullOrEmpty(updatedData.CategoryDescription))
                {
                    areaTypeCateFromDb.CategoryDescription = updatedData.CategoryDescription;
                }
                if (updatedData.Status != 0) // Giả sử 0 không phải giá trị hợp lệ
                {
                    areaTypeCateFromDb.Status = updatedData.Status;
                }
                else
                {
                    var response = new ResponseDTO<object>(400, "Giá trị Status không hợp lệ, phải là số nguyên khác 0!", null);
                    return BadRequest(response);
                }
                if (updatedData.Images != null)
                {
                    // Xử lý file upload (giả sử Images là IFormFile)
                    // Bạn cần thêm logic để lưu file vào hệ thống và cập nhật trường Images trong entity
                    // Ví dụ: areaTypeCateFromDb.Images = await SaveFile(updatedData.Images);
                }

                // Validate model sau khi cập nhật
                var areaTypeCateDTO = _mapper.Map<AreaTypeCategoryDTO>(areaTypeCateFromDb);
                TryValidateModel(areaTypeCateDTO);
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

                // Lưu thay đổi vào database
                await _areaTypeCategoryService.Update(areaTypeCateFromDb);

                // Trả về phản hồi thành công kèm dữ liệu đã cập nhật
                var updatedDTO = _mapper.Map<AreaTypeCategoryDTO>(areaTypeCateFromDb);
                var responseSuccess = new ResponseDTO<object>(200, "Cập nhật thành công!", updatedDTO);
                return Ok(responseSuccess);
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(500, $"Lỗi khi cập nhật dữ liệu: {ex.Message}", null);
                return StatusCode(500, response);
            }
        }
    }
}
