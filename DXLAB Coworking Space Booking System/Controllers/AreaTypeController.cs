using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> CreateAreaType([FromForm] AreaTypeDTO areTypeDto, [FromForm] List<IFormFile> files)
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
                var result = await ImageSerive.AddImage(files);
                if (!result.Item1)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Lỗi nhập ảnh", null));
                }

                foreach (var imageUrl in result.Item2)
                {
                    areaType.Images.Add(new Image { ImageUrl = imageUrl });
                }
                areTypeDto.Images = new List<string>();
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
        public async Task<IActionResult> GetAllAreaType(string fil)
        {
            try
            {
                var areaTypes = await _areaTypeService.GetAll();
                if (fil.Equals("1"))
                {
                    areaTypes = areaTypes.Where(x => x.IsDeleted == false);
                    var areaTypesDTO = _mapper.Map<IEnumerable<AreaTypeDTO>>(areaTypes);
                    var response = new ResponseDTO<object>(200, "Lấy thành công", areaTypesDTO);
                    return Ok(response);
                }
                else
                {
                    var areaTypesDTO = _mapper.Map<IEnumerable<AreaTypeDTO>>(areaTypes);
                    var response = new ResponseDTO<object>(200, "Lấy thành công", areaTypesDTO);
                    return Ok(response);
                }
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

        //[HttpPatch("{id}")]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> PatchRoom(int id, [FromBody] JsonPatchDocument<AreaType> patchDoc)
        //{
        //    try
        //    {
        //        if (patchDoc == null)
        //        {
        //            var response = new ResponseDTO<object>(400, "Bạn chưa truyền dữ liệu vào", null);
        //            return BadRequest(response);
        //        }
        //        var allowedPaths = new HashSet<string>
        //    {
        //               "areaTypeName",
        //                "areaDescription",
        //                "price",
        //                "images"

        //    };
        //        var areaTypeNameOp = patchDoc.Operations.FirstOrDefault(op => op.path.Equals("areaTypeName", StringComparison.OrdinalIgnoreCase));
        //        if(areaTypeNameOp != null)
        //        {
        //            var existedAreaType = await _areaTypeService.Get(x => x.AreaTypeName == areaTypeNameOp.value.ToString());
        //            if (existedAreaType != null)
        //            {
        //                var response = new ResponseDTO<object>(400, $"Tên loại phòng {areaTypeNameOp} đã tồn tại. Vui lòng nhập tên loại phòng khác!", null);
        //                return BadRequest(response);
        //            }
        //        }

        //        foreach (var operation in patchDoc.Operations)
        //        {
        //            if (!allowedPaths.Contains(operation.path))
        //            {
        //                var response1 = new ResponseDTO<object>(400, $"Không thể cập nhật trường: {operation.path}", null);
        //                return BadRequest(response1);
        //            }
        //        }

        //        var areaTypeFromDb = await _areaTypeService.Get(x => x.AreaTypeId == id);
        //        if (areaTypeFromDb == null)
        //        {
        //            var response2 = new ResponseDTO<object>(404, "Không tìm thấy phòng. Vui lòng nhập lại mã loại phòng!", null);
        //            return NotFound(response2);
        //        }

        //        patchDoc.ApplyTo(areaTypeFromDb, ModelState);

        //        if (!ModelState.IsValid)
        //        {
        //            var allErrors = ModelState
        //            .SelectMany(ms => ms.Value.Errors
        //            .Select(err => $"{ms.Key}: {err.ErrorMessage}"))
        //            .ToList();
        //            string errorString = string.Join(" | ", allErrors);
        //            var response = new ResponseDTO<object>(400, errorString, null);
        //            return BadRequest(response);
        //        }

        //        var areaTypeDTO = _mapper.Map<AreaTypeDTO>(areaTypeFromDb);

        //        bool isValid = TryValidateModel(areaTypeDTO);
        //        if (!isValid)
        //        {
        //            var allErrors = ModelState
        //            .SelectMany(ms => ms.Value.Errors
        //            .Select(err => $"{ms.Key}: {err.ErrorMessage}"))
        //            .ToList();

        //            string errorString = string.Join(" | ", allErrors);
        //            var response = new ResponseDTO<object>(404, errorString, null);
        //            return BadRequest(response);
        //        }
        //        await _areaTypeService.Update(areaTypeFromDb);
        //        var response3 = new ResponseDTO<object>(200, "Cập nhập thành công!", null);
        //        return Ok(response3);
        //    }
        //    catch (Exception ex)
        //    {
        //        var response = new ResponseDTO<object>(01, "Lỗi khi cập nhập dữ liệu!", null);
        //        return StatusCode(500, response);
        //    }
        //}

        [HttpPatch("{id}")]
        //[Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PatchRoom(int id, [FromForm] string areaTypeData, [FromForm] List<IFormFile> files)
        {
            try
            {
                if (string.IsNullOrEmpty(areaTypeData))
                {
                    var response = new ResponseDTO<object>(400, "Bạn chưa truyền dữ liệu vào", null);
                    return BadRequest(response);
                }

                // Chuyển đổi chuỗi JSON thành đối tượng DTO
                AreaTypePatchDTO patchDTO;
                try
                {
                    patchDTO = JsonConvert.DeserializeObject<AreaTypePatchDTO>(areaTypeData);
                }
                catch (JsonException)
                {
                    var response = new ResponseDTO<object>(400, "Dữ liệu không đúng định dạng JSON", null);
                    return BadRequest(response);
                }

                var areaTypeFromDb = await _areaTypeService.Get(x => x.AreaTypeId == id);
                if (areaTypeFromDb == null)
                {
                    var response = new ResponseDTO<object>(404, "Không tìm thấy phòng. Vui lòng nhập lại mã loại phòng!", null);
                    return NotFound(response);
                }

                // Kiểm tra xem tên loại phòng đã tồn tại chưa
                if (!string.IsNullOrEmpty(patchDTO.AreaTypeName) && patchDTO.AreaTypeName != areaTypeFromDb.AreaTypeName)
                {
                    var existedAreaType = await _areaTypeService.Get(x => x.AreaTypeName == patchDTO.AreaTypeName);
                    if (existedAreaType != null)
                    {
                        var response = new ResponseDTO<object>(400, $"Tên loại phòng {patchDTO.AreaTypeName} đã tồn tại. Vui lòng nhập tên loại phòng khác!", null);
                        return BadRequest(response);
                    }
                    areaTypeFromDb.AreaTypeName = patchDTO.AreaTypeName;
                }

                // Cập nhật các trường nếu có
                if (!string.IsNullOrEmpty(patchDTO.AreaDescription))
                {
                    areaTypeFromDb.AreaDescription = patchDTO.AreaDescription;
                }

                if (patchDTO.Price > 0)
                {
                    areaTypeFromDb.Price = patchDTO.Price;
                }

                // Gắn các trường vào đối tượng AreaType
                var areaTypeDTO = _mapper.Map<AreaTypeDTO>(areaTypeFromDb);
                bool isValid = TryValidateModel(areaTypeDTO);
                if (!isValid)
                {
                    var allErrors = ModelState
                        .SelectMany(ms => ms.Value.Errors
                        .Select(err => $"{ms.Key}: {err.ErrorMessage}"))
                        .ToList();
                    string errorString = string.Join(" | ", allErrors);
                    var response = new ResponseDTO<object>(400, errorString, null);
                    return BadRequest(response);
                }

                // Xử lý cập nhật hình ảnh nếu có
                await _areaTypeService.UpdateImage(id, areaTypeFromDb, files);
                await _areaTypeService.Update(areaTypeFromDb);

                var response3 = new ResponseDTO<object>(200, "Cập nhập thành công!", null);
                return Ok(response3);
            }
            catch (Exception ex)
            {
                var response = new ResponseDTO<object>(500, "Lỗi khi cập nhập dữ liệu!", null);
                return StatusCode(500, response);
            }
        }
    }
}