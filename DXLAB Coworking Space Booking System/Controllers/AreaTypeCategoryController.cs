using AutoMapper;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Core.DTOs.Room;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            _areaTypeCategoryService = areaTypeCategoryService ?? throw new ArgumentNullException(nameof(areaTypeCategoryService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost("newareatypecategory")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateAreaTypeCategory([FromForm] AreaTypeCategoryForAddDTO areaTypeCategoryDTO)
        {
            var areaTypeCategory = _mapper.Map<AreaTypeCategory>(areaTypeCategoryDTO);
            var result = await ImageSerive.AddImage(areaTypeCategoryDTO.Images); // Typo: Fix "ImageSerive" to "ImageService"
            if (!result.Item1)
            {
                return BadRequest(new ResponseDTO<object>(400, "Lỗi nhập ảnh", null));
            }

            foreach (var imageUrl in result.Item2)
            {
                areaTypeCategory.Images.Add(new Image { ImageUrl = imageUrl });
            }

            await _areaTypeCategoryService.Add(areaTypeCategory);
            return Ok();
        }

        [HttpGet("allAreaTypeCategory")]
        public async Task<IActionResult> GetAllAreaTypeCategory()
        {
            try
            {
                var areaTypeCategories = await _areaTypeCategoryService.GetAllWithInclude(x => x.Images);
                var areaTypeCategoryDTOs = _mapper.Map<IEnumerable<AreaTypeCategoryDTO>>(areaTypeCategories);
                var response = new ResponseDTO<object>(200, "Danh sách các loại: ", areaTypeCategoryDTOs);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, $"Lỗi database: {ex.Message}", null));
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAreaTypeCategory(int id, [FromForm] AreaTypeCategoryForUpdateDTO updatedData)
        {
            try
            {
                if (updatedData == null)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Bạn chưa truyền dữ liệu vào", null));
                }

                var areaTypeCateFromDb = await _areaTypeCategoryService.Get(x => x.CategoryId == id);
                if (areaTypeCateFromDb == null)
                {
                    return NotFound(new ResponseDTO<object>(404, "Không tìm thấy loại khu vực!", null));
                }

                // Check for duplicate title
                if (!string.IsNullOrEmpty(updatedData.Title) && updatedData.Title != areaTypeCateFromDb.Title)
                {
                    var existingCategory = await _areaTypeCategoryService.Get(x => x.Title == updatedData.Title && x.Status == 1);
                    if (existingCategory != null)
                    {
                        return BadRequest(new ResponseDTO<object>(400, $"Tên loại {updatedData.Title} đã tồn tại. Vui lòng nhập tên loại khác!", null));
                    }
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(updatedData.Title))
                    areaTypeCateFromDb.Title = updatedData.Title;

                if (!string.IsNullOrEmpty(updatedData.CategoryDescription))
                    areaTypeCateFromDb.CategoryDescription = updatedData.CategoryDescription;

                if (updatedData.Status != 0) // Assuming 0 is invalid
                    areaTypeCateFromDb.Status = updatedData.Status;
                else
                    return BadRequest(new ResponseDTO<object>(400, "Giá trị Status không hợp lệ, phải là số nguyên khác 0!", null));

                // Handle image updates
                var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                Directory.CreateDirectory(imagesDir); // Creates if it doesn't exist

                if (updatedData.Images != null && updatedData.Images.Any())
                {
                    areaTypeCateFromDb.Images.Clear(); // Clear existing images (if that's the intent)
                    foreach (var file in updatedData.Images)
                    {
                        if (file.Length > 0)
                        {
                            if (!file.FileName.EndsWith(".jpg") && !file.FileName.EndsWith(".png"))
                                return BadRequest(new ResponseDTO<object>(400, "Chỉ chấp nhận file .jpg hoặc .png!", null));

                            if (file.Length > 5 * 1024 * 1024)
                                return BadRequest(new ResponseDTO<object>(400, "File quá lớn, tối đa 5MB!", null));

                            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(file.FileName)}{Path.GetExtension(file.FileName)}";
                            var filePath = Path.Combine(imagesDir, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            areaTypeCateFromDb.Images.Add(new Image { ImageUrl = $"/images/{uniqueFileName}" });
                        }
                    }
                }

                // Validate updated entity
                var areaTypeCateDTO = _mapper.Map<AreaTypeCategoryDTO>(areaTypeCateFromDb);
                TryValidateModel(areaTypeCateDTO);
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.SelectMany(ms => ms.Value.Errors.Select(err => $"{ms.Key}: {err.ErrorMessage}")).ToList();
                    return BadRequest(new ResponseDTO<object>(400, string.Join(" | ", errors), null));
                }

                // Update in database (ensure this method exists and is implemented)
                await _areaTypeCategoryService.updateAreaTypeCategory(id, areaTypeCateFromDb); // Fixed method name casing

                var updatedDTO = _mapper.Map<AreaTypeCategoryDTO>(areaTypeCateFromDb);
                return Ok(new ResponseDTO<object>(200, "Cập nhật thành công!", updatedDTO));
            }
            catch (NotImplementedException ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Phương thức chưa được triển khai: {ex.Message}", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi cập nhật dữ liệu: {ex.Message}", null));
            }
        }
    }
}