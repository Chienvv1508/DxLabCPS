using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
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
        private readonly IImageServiceDb _imageServiceDb;

        public AreaTypeCategoryController(IAreaTypeCategoryService areaTypeCategoryService, IMapper mapper, IImageServiceDb imageServiceDb)
        {
            _areaTypeCategoryService = areaTypeCategoryService;
            _mapper = mapper;
            _imageServiceDb = imageServiceDb;
        }

        [HttpPost("newareatypecategory")]
        [Authorize(Roles = "Admin")]
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
                        "status"
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

        [HttpPost("newImage")]
        public async Task<IActionResult> AddNewImageInAreaTypeCategory(int id, [FromForm] List<IFormFile> Images)
        {

            try
            {
                var areaTypeCateFromDb = await _areaTypeCategoryService.Get(x => x.CategoryId == id);
                if (areaTypeCateFromDb == null)
                    return BadRequest(new ResponseDTO<object>(400, "Không tìm thấy loại này!", null));
                if (Images == null)
                    return BadRequest(new ResponseDTO<object>(400, "Bắt buộc nhập ảnh", null));
                var result = await ImageSerive.AddImage(Images);
                if (result.Item1 == true)
                {
                    foreach (var i in result.Item2)
                    {
                        areaTypeCateFromDb.Images.Add(new Image() { ImageUrl = i });

                    }
                }
                else
                    return BadRequest(new ResponseDTO<object>(400, "Cập nhập lỗi!", null));

                await _areaTypeCategoryService.Update(areaTypeCateFromDb);
                return Ok(new ResponseDTO<object>(200, "Cập nhập thành công", null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, "Cập nhập lỗi!", null));
            }
        }


        [HttpDelete("Images")]
        public async Task<IActionResult> RemoveImage(int id,[FromBody] List<string> images)
        {
            try
            {
                var areaTypeCateFromDb = await _areaTypeCategoryService.GetWithInclude(x => x.CategoryId == id,x => x.Images);
                if (areaTypeCateFromDb == null)
                    return BadRequest(new ResponseDTO<object>(400, "Không tìm thấy loại này!", null));
                if(images == null)
                    return BadRequest(new ResponseDTO<object>(400, "Bắt buộc nhập ảnh", null));
                var imageList = areaTypeCateFromDb.Images;
                if(imageList.Count <= images.Count)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Không được xóa hết ảnh", null));
                }
                
                foreach(var image in images)
                {
                    var item = imageList.FirstOrDefault(x => x.ImageUrl == $"{image}");
                    if(item == null)
                        return BadRequest(new ResponseDTO<object>(400, "Ảnh không tồn tại trong loại khu vực!", null));
                    areaTypeCateFromDb.Images.Remove(item);

                }
                await _areaTypeCategoryService.UpdateImage(areaTypeCateFromDb,images);
               

                foreach (var image in images)
                {
                    ImageSerive.RemoveImage(image);
                }
                return Ok(new ResponseDTO<object>(200, "Cập nhập thành công!", null));

            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, "Cập nhập lỗi!", null));
            }
        }

    }
}
