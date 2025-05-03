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

        public AreaTypeCategoryController(IAreaTypeCategoryService areaTypeCategoryService, IMapper mapper)
        {
            _areaTypeCategoryService = areaTypeCategoryService;
            _mapper = mapper;
        }

        [HttpPost("newareatypecategory")]
        [Authorize(Roles = "Admin")]
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
            ResponseDTO<AreaTypeCategory> resultAdd = await _areaTypeCategoryService.AddNewImage(id, images);
            return StatusCode(resultAdd.StatusCode, resultAdd);
        }


        [HttpDelete("Images")]
        public async Task<IActionResult> RemoveImage(int id, [FromBody] List<string> images)
        {
            ResponseDTO<AreaTypeCategory> resultRemove = await _areaTypeCategoryService.RemoveImages(id, images);
            return StatusCode(resultRemove.StatusCode, resultRemove);
        }

    }
}
