using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/areatype")]
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
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateAreaType([FromForm] AreaTypeForAddDTO areTypeDto)
        {
            ResponseDTO<AreaType> result = await _areaTypeService.CreateNewAreaType(areTypeDto);
            if (result.StatusCode == 200)
            {
                var areTypeDtoResult = _mapper.Map<AreaTypeDTO>(result.Data);
                var response = new ResponseDTO<AreaTypeDTO>(201, "Tạo kiểu khu vực thành công", areTypeDtoResult);
                return CreatedAtAction(nameof(GetAreTypeById), new { id = result.Data.AreaTypeId }, response);
            }
            return StatusCode(result.StatusCode, result);


        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAreTypeById(int id)
        {
            try
            {
                var areaType = await _areaTypeService.Get(x => x.AreaTypeId == id && x.Status == 1);
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
                var response = new ResponseDTO<object>(01, "Lỗi khi lấy tất cả kiểu khu vực", null);
                return StatusCode(500, response);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAreaType(string fil)
        {

            ResponseDTO<List<AreaType>> result = await _areaTypeService.GetAllByFilPara(fil);
           if(result.StatusCode == 200)
            {
                var listAreaTypeDTO = _mapper.Map<List<AreaTypeDTO>>(result.Data);
                var response = new ResponseDTO<object>(200, "Lấy thành công", listAreaTypeDTO);
                return Ok(response);
            }
            
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        //[Consumes("multipart/form-data")]
        public async Task<IActionResult> PatchRoom(int id, [FromBody] JsonPatchDocument<AreaType> patchDoc)
        {

            ResponseDTO<AreaType> result = await _areaTypeService.PatchAreaType(id, patchDoc);
            return StatusCode(result.StatusCode, result);
        }


        [HttpPost("newImage")]
        public async Task<IActionResult> AddNewImage(int id, [FromForm] List<IFormFile> images)
        {
            ResponseDTO<AreaType> result = await _areaTypeService.AddImages(id, images);
            return StatusCode(result.StatusCode, result);
        }


        [HttpDelete("Images")]
        public async Task<IActionResult> RemoveImage(int id, [FromBody] List<string> images)
        {
            ResponseDTO<AreaType> result = await _areaTypeService.RemoveImages(id, images);
            return StatusCode(result.StatusCode, result);
        }
    }
}