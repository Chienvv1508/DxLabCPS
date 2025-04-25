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
        //private readonly IImageServiceDb _imageServiceDb;
        //private readonly IAreaTypeService _areaTypeService;

        public AreaTypeCategoryController(IAreaTypeCategoryService areaTypeCategoryService, IMapper mapper/*, IImageServiceDb imageServiceDb, IAreaTypeService areaTypeService*/)
        {
            _areaTypeCategoryService = areaTypeCategoryService;
            _mapper = mapper;
            //_imageServiceDb = imageServiceDb;
            //_areaTypeService = areaTypeService;
        }

        [HttpPost("newareatypecategory")]
        //[Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateAreaTypeCategory([FromForm] AreaTypeCategoryForAddDTO areaTypeCategoryDTO)
        {
            var result = await _areaTypeCategoryService.CreateNewAreaTypeCategoory(areaTypeCategoryDTO);
            return StatusCode(result.StatusCode, result.Message);
        }

        [HttpGet("allAreaTypeCategory")]
        public async Task<IActionResult> GetAllAreaTyepCategory()
        {
            try
            {
                var areaTypeCategorys = await _areaTypeCategoryService.GetAllWithInclude(x => x.Images);
                areaTypeCategorys = areaTypeCategorys.Where(x => x.Status == 1);
                var areaTypeCategoryDTOS = _mapper.Map<IEnumerable<AreaTypeCategoryDTO>>(areaTypeCategorys);
                var response = new ResponseDTO<object>(200, "Danh sách các loại: ", areaTypeCategoryDTOS);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, "Lỗi cơ sở dữ liệu!", null));
            }

        }
        [HttpPatch()]
        public async Task<IActionResult> PatchAreaTypeCategory(int id, [FromBody] JsonPatchDocument<AreaTypeCategory> patchDoc)
        {
            ResponseDTO<AreaTypeCategory> result = await _areaTypeCategoryService.PatchAreaTypeCategory(id, patchDoc);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(result.StatusCode, result.Message);
            }

        }

        [HttpPost("newImage")]
        public async Task<IActionResult> AddNewImageInAreaTypeCategory(int id, [FromForm] List<IFormFile> images)
        {
            //try
            //{
            ResponseDTO<AreaTypeCategory> resultAdd = await _areaTypeCategoryService.AddNewImage(id, images);
            return StatusCode(resultAdd.StatusCode, resultAdd);

            //    var areaTypeCateFromDb = await _areaTypeCategoryService.Get(x => x.CategoryId == id && x.Status == 1);
            //    if (areaTypeCateFromDb == null)
            //        return BadRequest(new ResponseDTO<object>(400, "Không tìm thấy loại này!", null));
            //    if (images == null)
            //        return BadRequest(new ResponseDTO<object>(400, "Bắt buộc nhập ảnh", null));
            //    var result = await ImageSerive.AddImage(images);
            //    if (result.Item1 == true)
            //    {
            //        foreach (var i in result.Item2)
            //        {
            //            areaTypeCateFromDb.Images.Add(new Image() { ImageUrl = i });

            //        }
            //    }
            //    else
            //        return BadRequest(new ResponseDTO<object>(400, "Cập nhập lỗi!", null));

            //    await _areaTypeCategoryService.Update(areaTypeCateFromDb);
            //    return Ok(new ResponseDTO<object>(200, "Cập nhập thành công", null));
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(new ResponseDTO<object>(400, "Cập nhập lỗi!", null));
            //}
        }


        [HttpDelete("Images")]
        public async Task<IActionResult> RemoveImage(int id, [FromBody] List<string> images)
        {
            ResponseDTO<AreaTypeCategory> resultRemove = await _areaTypeCategoryService.RemoveImages(id, images);
            return StatusCode(resultRemove.StatusCode, resultRemove);


            //try
            //{
            //    var areaTypeCateFromDb = await _areaTypeCategoryService.GetWithInclude(x => x.CategoryId == id && x.Status == 1, x => x.Images);
            //    if (areaTypeCateFromDb == null)
            //        return BadRequest(new ResponseDTO<object>(400, "Không tìm thấy loại này!", null));
            //    if (images == null)
            //        return BadRequest(new ResponseDTO<object>(400, "Bắt buộc nhập ảnh", null));
            //    var imageList = areaTypeCateFromDb.Images;
            //    foreach (var image in images)
            //    {
            //        var item = imageList.FirstOrDefault(x => x.ImageUrl == $"{image}");
            //        if (item == null)
            //            return BadRequest(new ResponseDTO<object>(400, "Ảnh không tồn tại trong loại khu vực!", null));
            //        areaTypeCateFromDb.Images.Remove(item);

            //    }
            //    foreach (var image in images)
            //    {
            //        ImageSerive.RemoveImage(image);
            //    }
            //    await _areaTypeCategoryService.UpdateImage(areaTypeCateFromDb, images);
            //    return Ok(new ResponseDTO<object>(200, "Cập nhập thành công!", null));

            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(new ResponseDTO<object>(400, "Cập nhập lỗi!", null));
            //}
        }

    }
}
