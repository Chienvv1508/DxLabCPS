using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AreaTypeService : IAreaTypeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AreaTypeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(AreaType entity)
        {
            await _unitOfWork.AreaTypeRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<AreaType> Get(Expression<Func<AreaType, bool>> expression)
        {
            return await _unitOfWork.AreaTypeRepository.GetWithInclude(expression, x => x.Images);

        }

        public async Task<IEnumerable<AreaType>> GetAll()
        {
            return await _unitOfWork.AreaTypeRepository.GetAllWithInclude(x => x.Images);
        }

        public async Task<IEnumerable<AreaType>> GetAll(Expression<Func<AreaType, bool>> expression)
        {
            return await _unitOfWork.AreaTypeRepository.GetAll(expression);
        }

        public async Task<AreaType> GetById(int id)
        {
            return await _unitOfWork.AreaTypeRepository.GetById(id);
        }

        public async Task Update(AreaType entity)
        {
            await _unitOfWork.AreaTypeRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }
        public async Task<object> GetAreaTypeForAddRoom()
        {
            var listAreaType = await _unitOfWork.AreaTypeRepository.GetAll(x => x.Status == 1);
            var listAreaTypeResult = listAreaType.Select(x => new AreaAddDTO() { AreaTypeId = x.AreaTypeId, AreaTypeName = x.AreaTypeName, Size = x.Size });
            return listAreaTypeResult;
        }

        public async Task<IEnumerable<AreaType>> GetAllWithInclude(params Expression<Func<AreaType, object>>[] includes)
        {
            return await _unitOfWork.AreaTypeRepository.GetAllWithInclude(includes);
        }

        public async Task<AreaType> GetWithInclude(Expression<Func<AreaType, bool>> expression, params Expression<Func<AreaType, object>>[] includes)
        {
            return await _unitOfWork.AreaTypeRepository.GetWithInclude(expression, includes);
        }

        public async Task UpdateImage(AreaType areaTypeFromDb, List<string> images)
        {
            try
            {
                var listImage = await _unitOfWork.ImageRepository.GetAll(x => x.AreaTypeId == areaTypeFromDb.AreaTypeId);
                if (images == null)
                    throw new ArgumentNullException();
                foreach (var item in images)
                {
                    var x = listImage.FirstOrDefault(x => x.ImageUrl == item);
                    if (x == null) throw new Exception("Ảnh nhập vào không phù hợp");
                    await _unitOfWork.ImageRepository.Delete(x.ImageId);

                }
                await _unitOfWork.AreaTypeRepository.Update(areaTypeFromDb);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }
        }

        public async Task<ResponseDTO<AreaType>> CreateNewAreaType(AreaTypeForAddDTO areTypeDto)
        {
            try
            {
                bool checkValidAreaType = await CheckValidAreaTypeForAdd(areTypeDto);
                if (checkValidAreaType == false)
                    return new ResponseDTO<AreaType>(400, "Dữ liệu đầu vào không phù hợp!", null);
                IMapper mapper = GenerateMapper.GenerateMapperForService();
                var areaType = mapper.Map<AreaType>(areTypeDto);
                await AddImages(areTypeDto, areaType);
                areaType.Status = 1;
                await _unitOfWork.AreaTypeRepository.Add(areaType);
                await _unitOfWork.CommitAsync();
                return new ResponseDTO<AreaType>(200, "Tạo kiểu khu vực thành công", areaType);

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return new ResponseDTO<AreaType>(400, "Tạo kiểu khu vực thất bại!", null);
            }


        }

        private async Task AddImages(AreaTypeForAddDTO areTypeDto, AreaType areaType)
        {
            if (areTypeDto != null && areaType != null)
            {
                var rs = await ImageSerive.AddImage(areTypeDto.Images);
                if (rs.Item1 == true)
                {
                    foreach (var i in rs.Item2)
                    {
                        areaType.Images.Add(new Image() { ImageUrl = i });
                    }
                }
            }
        }

        private async Task<bool> CheckValidAreaTypeForAdd(AreaTypeForAddDTO areTypeDto)
        {
            if (areTypeDto == null)
                return false;
            var checkValid = ValidationModel<AreaTypeForAddDTO>.ValidateModel(areTypeDto);
            if (checkValid.Item1 == false)
            {
                return false;
            }
            var existedAreaType = await _unitOfWork.AreaTypeRepository.Get(x => x.AreaTypeName == areTypeDto.AreaTypeName && x.Status == 1);
            if (existedAreaType != null)
            {

                return false;
            }
            return true;
        }

        public async Task<ResponseDTO<List<AreaType>>> GetAllByFilPara(string fil)
        {
            try
            {
                if (string.IsNullOrEmpty(fil))
                    return new ResponseDTO<List<AreaType>>(400, "Tham số lọc không được để trống", null);
                var areaTypes = await _unitOfWork.AreaTypeRepository.GetAll();
                if (fil.Equals("1"))
                {
                    areaTypes = areaTypes.Where(x => x.Status == 1);
                    //var areaTypesDTO = _mapper.Map<IEnumerable<AreaTypeDTO>>(areaTypes);
                    var response = new ResponseDTO<List<AreaType>>(200, "Lấy thành công", areaTypes.ToList());
                    return response;
                }
                else
                {
                    return new ResponseDTO<List<AreaType>>(400, "Tham số nhập không phù hợp! Vui lòng nhập lại!", areaTypes.ToList());
                }
            }
            catch (Exception ex)
            {
                return new ResponseDTO<List<AreaType>>(400, "Lỗi lấy danh sách loại khu vực!", null);
            }
        }

        public async Task<ResponseDTO<AreaType>> PatchAreaType(int id, JsonPatchDocument<AreaType> patchDoc)
        {
            try
            {
                Tuple<bool, string, AreaType> checkInputAndGetAreaType = await CheckInputAndGetAreaType(id, patchDoc);
                if (checkInputAndGetAreaType.Item1 == false)
                {
                    return new ResponseDTO<AreaType>(400, checkInputAndGetAreaType.Item2, null);
                }
                var araeTypeFromDb = checkInputAndGetAreaType.Item3;

                Tuple<bool, string> updateAreaType = await UpdateAreaType(patchDoc, araeTypeFromDb);
                if (updateAreaType.Item1 == false)
                {
                    return new ResponseDTO<AreaType>(400, updateAreaType.Item2, null);
                }
                return new ResponseDTO<AreaType>(200, updateAreaType.Item2, null);

                //    try
                //    {
                //        if (patchDoc == null)
                //        {
                //            var response = new ResponseDTO<object>(400, "Bạn chưa truyền dữ liệu vào", null);
                //            return BadRequest(response);
                //        }
                //        var allowedPaths = new HashSet<string>
                //{
                //           "areaTypeName",
                //            "areaDescription",
                //            "price"
                //};
                //        var areaTypeNameOp = patchDoc.Operations.FirstOrDefault(op => op.path.Equals("areaTypeName", StringComparison.OrdinalIgnoreCase));
                //        if (areaTypeNameOp != null)
                //        {
                //            var existedAreaType = await _areaTypeService.Get(x => x.AreaTypeName == areaTypeNameOp.value.ToString());
                //            if (existedAreaType != null)
                //            {
                //                var response = new ResponseDTO<object>(400, $"Tên kiểu khu vực {areaTypeNameOp} đã tồn tại. Vui lòng nhập tên kiểu khu vực khác!", null);
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
                //            var response2 = new ResponseDTO<object>(404, "Không tìm thấy kiểu khu vực. Vui lòng nhập lại mã kiểu khu vực!", null);
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

                //        // Gắn các trường vào đối tượng AreaType
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
            }
            catch (Exception ex)
            {
                return new ResponseDTO<AreaType>(400, "Cập nhập loại khu vực thất bại!", null);
            }
        }

        private async Task<Tuple<bool, string>> UpdateAreaType(JsonPatchDocument<AreaType> patchDoc, AreaType araeTypeFromDb)
        {
            try
            {
                if (araeTypeFromDb == null || araeTypeFromDb == null)
                    return new Tuple<bool, string>(false, "Lỗi nhập thông tin cập nhập loại khu vực!");
                patchDoc.ApplyTo(araeTypeFromDb);

                IMapper mapper = GenerateMapper.GenerateMapperForService();

                var areaTypeDTO = mapper.Map<AreaTypeDTO>(araeTypeFromDb);
                var isValid = ValidationModel<AreaTypeDTO>.ValidateModel(areaTypeDTO);
                if (isValid.Item1 == false)
                {
                    return new Tuple<bool, string>(false, isValid.Item2);
                }
                await _unitOfWork.AreaTypeRepository.Update(araeTypeFromDb);
                await _unitOfWork.CommitAsync();
                return new Tuple<bool, string>(true, "Cập nhập thành công!");

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return new Tuple<bool, string>(false, "Lỗi cập nhập loại khu vực!");
            }
        }

        private async Task<Tuple<bool, string, AreaType>> CheckInputAndGetAreaType(int id, JsonPatchDocument<AreaType> patchDoc)
        {
            if (patchDoc == null)
            {
                return new Tuple<bool, string, AreaType>(false, "Bạn chưa truyền dữ liệu vào", null);
            }
            var areaTypeFromDb = await _unitOfWork.AreaTypeRepository.Get(x => x.AreaTypeId == id && x.Status == 1);
            if (areaTypeFromDb == null)
            {
                return new Tuple<bool, string, AreaType>(false, "Không tìm thấy kiểu khu vực. Vui lòng nhập lại mã kiểu khu vực!", null);
            }
            var allowedPaths = new HashSet<string>
            {
                       "areaTypeName",
                        "areaDescription",
                        "price"
            };
            foreach (var operation in patchDoc.Operations)
            {
                if (!allowedPaths.Contains(operation.path))
                {
                    return new Tuple<bool, string, AreaType>(false, $"Không thể cập nhật trường: {operation.path}", null);
                }
            }
            var areaTypeNameOp = patchDoc.Operations.FirstOrDefault(op => op.path.Equals("areaTypeName", StringComparison.OrdinalIgnoreCase));
            if (areaTypeNameOp != null)
            {
                var existedAreaType = await _unitOfWork.AreaTypeRepository.Get(x => x.AreaTypeName == areaTypeNameOp.value.ToString() && x.Status == 1);
                if (existedAreaType != null)
                {
                    return new Tuple<bool, string, AreaType>(false, $"Tên kiểu khu vực {areaTypeNameOp} đã tồn tại. Vui lòng nhập tên kiểu khu vực khác!", null);
                }
            }

            return new Tuple<bool, string, AreaType>(true, "", areaTypeFromDb);




        }

        public async Task<ResponseDTO<AreaType>> AddImages(int id, List<IFormFile> images)
        {
            try
            {
                var areaTypeFromDb = await _unitOfWork.AreaTypeRepository.Get(x => x.AreaTypeId == id && x.Status == 1);
                if (areaTypeFromDb == null)
                    return new ResponseDTO<AreaType>(400, "Không tìm thấy kiểu này!", null);
                if (images == null)
                    return new ResponseDTO<AreaType>(400, "Bắt buộc nhập ảnh", null);
                var result = await ImageSerive.AddImage(images);
                if (result.Item1 == true)
                {
                    foreach (var i in result.Item2)
                    {
                        areaTypeFromDb.Images.Add(new Image() { ImageUrl = i });

                    }
                }
                else
                    return new ResponseDTO<AreaType>(400, "Cập nhập lỗi!", null);

                await _unitOfWork.AreaTypeRepository.Update(areaTypeFromDb);
                await _unitOfWork.CommitAsync();
                return new ResponseDTO<AreaType>(200, "Cập nhập thành công", null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return new ResponseDTO<AreaType>(400, "Cập nhập lỗi!", null);
            }
        }

        public async Task<ResponseDTO<AreaType>> RemoveImages(int id, List<string> images)
        {
            try
            {
                var areaTypeFromDb = await _unitOfWork.AreaTypeRepository.GetWithInclude(x => x.AreaTypeId == id && x.Status == 1, x => x.Images);
                if (areaTypeFromDb == null)
                    return new ResponseDTO<AreaType>(400, "Không tìm thấy kiểu này!", null);
                if (images == null)
                    return new ResponseDTO<AreaType>(400, "Bắt buộc nhập ảnh", null);
                var imageList = areaTypeFromDb.Images;
                foreach (var image in images)
                {
                    var item = imageList.FirstOrDefault(x => x.ImageUrl == $"{image}");
                    if (item == null)
                        return new ResponseDTO<AreaType>(400, "Ảnh không tồn tại trong kiểu khu vực!", null);
                    areaTypeFromDb.Images.Remove(item);
                }
                await UpdateImage(areaTypeFromDb, images);


                foreach (var image in images)
                {
                    ImageSerive.RemoveImage(image);
                }
                return new ResponseDTO<AreaType>(200, "Cập nhập thành công!", null);

            }
            catch (Exception ex)
            {
                return new ResponseDTO<AreaType>(400, "Cập nhập lỗi!", null);
            }
        }
    }
}
