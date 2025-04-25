using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DxLabCoworkingSpaceForService;
using NBitcoin.Secp256k1;
using Nethereum.Contracts.QueryHandlers.MultiCall;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;

namespace DxLabCoworkingSpace
{
    public class AreaTypeCategoryService : IAreaTypeCategoryService
    {
        private readonly IUnitOfWork _unitOfWork;



        public AreaTypeCategoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(AreaTypeCategory entity)
        {
            await _unitOfWork.AreaTypeCategoryRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task<ResponseDTO<AreaTypeCategoryForAddDTO>> CreateNewAreaTypeCategoory(AreaTypeCategoryForAddDTO areaTypeCategoryDTO)
        {
            try
            {
                var checkValid = ValidationModel<AreaTypeCategoryForAddDTO>.ValidateModel(areaTypeCategoryDTO);
                if (checkValid.Item1 == false)
                {
                    return new ResponseDTO<AreaTypeCategoryForAddDTO>(400, checkValid.Item2, null);
                }
                var existedName = await _unitOfWork.AreaTypeCategoryRepository.Get(x => x.Title == areaTypeCategoryDTO.Title && x.Status == 1);
                if (existedName != null)
                {
                    return new ResponseDTO<AreaTypeCategoryForAddDTO>(400, $"Đã có tên loại dịch vụ:{areaTypeCategoryDTO.Title} trong cơ sở dữ liệu!", null);
                }

                IMapper mapper = GenerateMapper.GenerateMapperForService();
                var areaTypeCategory = mapper.Map<AreaTypeCategory>(areaTypeCategoryDTO);
                var result = await ImageSerive.AddImage(areaTypeCategoryDTO.Images);
                if (!result.Item1)
                {
                    return new ResponseDTO<AreaTypeCategoryForAddDTO>(400, "Lỗi nhập ảnh", null);
                }

                foreach (var imageUrl in result.Item2)
                {
                    areaTypeCategory.Images.Add(new Image { ImageUrl = imageUrl });
                }
                areaTypeCategory.Status = 1;
                await _unitOfWork.AreaTypeCategoryRepository.Add(areaTypeCategory);
                await _unitOfWork.CommitAsync();
                return new ResponseDTO<AreaTypeCategoryForAddDTO>(201, "Thêm dịch vụ thành công!", null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return new ResponseDTO<AreaTypeCategoryForAddDTO>(500, "Thêm dịch vụ thất bại", null);
            }
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<AreaTypeCategory> Get(Expression<Func<AreaTypeCategory, bool>> expression)
        {
            return await _unitOfWork.AreaTypeCategoryRepository.Get(expression);

        }

        public async Task<IEnumerable<AreaTypeCategory>> GetAll()
        {
            return await _unitOfWork.AreaTypeCategoryRepository.GetAll();
        }

        public async Task<IEnumerable<AreaTypeCategory>> GetAll(Expression<Func<AreaTypeCategory, bool>> expression)
        {
            return await _unitOfWork.AreaTypeCategoryRepository.GetAll(expression);
        }

        public async Task<IEnumerable<AreaTypeCategory>> GetAllWithInclude(params Expression<Func<AreaTypeCategory, object>>[] includes)
        {
            return await _unitOfWork.AreaTypeCategoryRepository.GetAllWithInclude(includes);
        }

        public Task<AreaTypeCategory> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<AreaTypeCategory> GetWithInclude(Expression<Func<AreaTypeCategory, bool>> expression, params Expression<Func<AreaTypeCategory, object>>[] includes)
        {

            return await _unitOfWork.AreaTypeCategoryRepository.GetWithInclude(expression, includes);

        }

        public async Task<ResponseDTO<AreaTypeCategory>> PatchAreaTypeCategory(int id, JsonPatchDocument<AreaTypeCategory> patchDoc)
        {
            try
            {
                Tuple<bool, string, AreaTypeCategory> checkValidAreaTypeCatgoryAndPatchDoc = await CheckValidAreaTypeCatgoryAndPatchDoc(id, patchDoc);
                if (checkValidAreaTypeCatgoryAndPatchDoc.Item1 == false)
                {
                    return new ResponseDTO<AreaTypeCategory>(400, checkValidAreaTypeCatgoryAndPatchDoc.Item2, null);
                }
                var areaTypeCateFromDb = checkValidAreaTypeCatgoryAndPatchDoc.Item3;


                Tuple<bool, string> patch = await UpdateAreaTypeCategory(patchDoc, areaTypeCateFromDb);
                if (patch.Item1 == false)
                {
                    return new ResponseDTO<AreaTypeCategory>(400, patch.Item2, null);
                }

                _unitOfWork.CommitAsync();

                return new ResponseDTO<AreaTypeCategory>(200, "Cập nhập thành công!", null);


            }
            catch (Exception ex)
            {
                _unitOfWork.RollbackAsync();
                return new ResponseDTO<AreaTypeCategory>(500, "Lỗi khi cập nhập dữ liệu!", null);

            }
        }

        private async Task<Tuple<bool, string>> UpdateAreaTypeCategory(JsonPatchDocument<AreaTypeCategory> patchDoc, AreaTypeCategory areaTypeCateFromDb)
        {
            try
            {
                if (patchDoc == null || areaTypeCateFromDb == null)
                {
                    return new Tuple<bool, string>(false, "Phải bắt buộc nhập dữ liệu");
                }
                patchDoc.ApplyTo(areaTypeCateFromDb);

                IMapper mapper = GenerateMapper.GenerateMapperForService();
                var areTypeCates = mapper.Map<AreaTypeCategoryDTO>(areaTypeCateFromDb);

                var checkModel = ValidationModel<AreaTypeCategoryDTO>.ValidateModel(areTypeCates);
                if (checkModel.Item1 == false)
                {
                    return new Tuple<bool, string>(false, checkModel.Item2);
                }
                await _unitOfWork.AreaTypeCategoryRepository.Update(areaTypeCateFromDb);
                return new Tuple<bool, string>(true, "");

            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, "Lỗi cập nhập!");
            }
        }

        private async Task<Tuple<bool, string, AreaTypeCategory>> CheckValidAreaTypeCatgoryAndPatchDoc(int id, JsonPatchDocument<AreaTypeCategory> patchDoc)
        {
            try
            {
                if (patchDoc == null)
                {

                    return new Tuple<bool, string, AreaTypeCategory>(false, "Bạn chưa truyền dữ liệu vào", null);
                }
                var areaTypeCateFromDb = await _unitOfWork.AreaTypeCategoryRepository.Get(x => x.CategoryId == id && x.Status == 1);
                if (areaTypeCateFromDb == null)
                {

                    return new Tuple<bool, string, AreaTypeCategory>(false, "Không tìm loại dịch vụ!", null);
                }

                var allowedPaths = new HashSet<string>
                {
                       "title",
                        "categoryDescription"
                };
                foreach (var operation in patchDoc.Operations)
                {
                    if (!allowedPaths.Contains(operation.path))
                    {
                        var response1 = new ResponseDTO<object>(400, $"Không thể cập nhật trường: {operation.path}", null);
                        return new Tuple<bool, string, AreaTypeCategory>(false, $"Không thể cập nhật trường: {operation.path}", null);
                    }
                }

                var areaTypeCategoryTitleOp = patchDoc.Operations.FirstOrDefault(op => op.path.Equals("title", StringComparison.OrdinalIgnoreCase));
                if (areaTypeCategoryTitleOp != null)
                {
                    var existedAreaTypeCategory = await _unitOfWork.AreaTypeCategoryRepository.Get(x => x.Title == areaTypeCategoryTitleOp.value.ToString() && x.Status == 1);
                    if (existedAreaTypeCategory != null)
                    {

                        return new Tuple<bool, string, AreaTypeCategory>(false, $"Tên loại {areaTypeCategoryTitleOp.value.ToString()} đã tồn tại. Vui lòng nhập tên loại phòng khác!", null);
                    }
                }
                return new Tuple<bool, string, AreaTypeCategory>(true, "", areaTypeCateFromDb);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, AreaTypeCategory>(false, $"Lỗi cập nhập!", null);
            }


        }

        public async Task Update(AreaTypeCategory entity)
        {
            await _unitOfWork.AreaTypeCategoryRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }

        //public async Task updateAreaTypeCategory(int id, AreaTypeCategory updatedAreaTypeCategory, List<IFormFile> newImages)
        //{
        //    var areaTypeCategory = await GetWithInclude(b => b.CategoryId == id, x => x.Images);
        //    if (areaTypeCategory == null)
        //    {
        //        throw new Exception("Không tìm thấy AreaType");
        //    }

        //    // Xóa ảnh cũ
        //    if (areaTypeCategory.Images != null && areaTypeCategory.Images.Any())
        //    {
        //        foreach (var image in areaTypeCategory.Images.ToList())
        //        {
        //            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
        //            if (File.Exists(filePath))
        //            {
        //                File.Delete(filePath);
        //            }
        //            _unitOfWork.Context.Set<Image>().Remove(image);
        //        }
        //        areaTypeCategory.Images.Clear();
        //    }

        //    // Thêm ảnh mới từ newImages
        //    if (newImages != null && newImages.Any())
        //    {
        //        var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
        //        Directory.CreateDirectory(imagesDir);

        //        foreach (var file in newImages)
        //        {
        //            if (file.Length > 0)
        //            {
        //                if (!file.FileName.EndsWith(".jpg") && !file.FileName.EndsWith(".png"))
        //                    throw new Exception("Chỉ chấp nhận file .jpg hoặc .png!");
        //                if (file.Length > 5 * 1024 * 1024)
        //                    throw new Exception("File quá lớn, tối đa 5MB!");

        //                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(file.FileName)}{Path.GetExtension(file.FileName)}";
        //                var filePath = Path.Combine(imagesDir, uniqueFileName);

        //                using (var stream = new FileStream(filePath, FileMode.Create))
        //                {
        //                    await file.CopyToAsync(stream);
        //                }

        //                areaTypeCategory.Images.Add(new Image { ImageUrl = $"/images/{uniqueFileName}" });
        //            }
        //        }
        //    }

        //    // Cập nhật thông tin khác
        //    areaTypeCategory.Title = updatedAreaTypeCategory.Title;
        //    areaTypeCategory.CategoryDescription = updatedAreaTypeCategory.CategoryDescription;

        //    // Lưu thay đổi
        //    await _unitOfWork.AreaTypeCategoryRepository.Update(areaTypeCategory);
        //    await _unitOfWork.CommitAsync();
        //}

        public async Task UpdateImage(AreaTypeCategory areaTypeCateFromDb, List<string> images)
        {
            try
            {
                var listImage = await _unitOfWork.ImageRepository.GetAll(x => x.AreaTypeCategoryId == areaTypeCateFromDb.CategoryId);
                if (images == null)
                    throw new ArgumentNullException();

                //var imageList = areaTypeCateFromDb.Images;
                //foreach (var image in images)
                //{
                //    var item = imageList.FirstOrDefault(x => x.ImageUrl == $"{image}");
                //    //if (item == null)
                //    //    return new ResponseDTO<AreaTypeCategory>(400, "Ảnh không tồn tại trong loại khu vực!", null);
                //    areaTypeCateFromDb.Images.Remove(item);

                //}
                foreach (var item in images)
                {
                    var x = listImage.FirstOrDefault(x => x.ImageUrl == item);
                    if (x == null) throw new Exception("Ảnh nhập vào không phù hợp");
                    await _unitOfWork.ImageRepository.Delete(x.ImageId);


                }

                await _unitOfWork.AreaTypeCategoryRepository.Update(areaTypeCateFromDb);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }
        }

        public async Task<ResponseDTO<AreaTypeCategory>> AddNewImage(int id, List<IFormFile> images)
        {
            try
            {
                if (images == null || images.Count == 0)
                    return new ResponseDTO<AreaTypeCategory>(400, "Bạn phải nhập ảnh", null);
                var areaTypeCateFromDb = await _unitOfWork.AreaTypeCategoryRepository.Get(x => x.CategoryId == id && x.Status == 1);
                if (areaTypeCateFromDb == null)
                    return new ResponseDTO<AreaTypeCategory>(400, "Không tìm thấy loại này!", null);
                var result = await ImageSerive.AddImage(images);
                if (result.Item1 == true)
                {
                    foreach (var i in result.Item2)
                    {
                        areaTypeCateFromDb.Images.Add(new Image() { ImageUrl = i });

                    }
                }
                else
                    return new ResponseDTO<AreaTypeCategory>(400, "Cập nhập lỗi!", null);

                await _unitOfWork.AreaTypeCategoryRepository.Update(areaTypeCateFromDb);
                await _unitOfWork.CommitAsync();
                return new ResponseDTO<AreaTypeCategory>(200, "Cập nhập thành công", null);
            }
            catch (Exception ex)
            {

                await _unitOfWork.RollbackAsync();
                return new ResponseDTO<AreaTypeCategory>(500, "Không cập nhập được ảnh", null);
            }
        }

        public async Task<ResponseDTO<AreaTypeCategory>> RemoveImages(int id, List<string> images)
        {
            try
            {
                var areaTypeCateFromDb = await _unitOfWork.AreaTypeCategoryRepository.GetWithInclude(x => x.CategoryId == id && x.Status == 1, x => x.Images);
                if (areaTypeCateFromDb == null)
                    return new ResponseDTO<AreaTypeCategory>(400, "Không tìm thấy loại này!", null);
                if (images == null)
                    return new ResponseDTO<AreaTypeCategory>(400, "Bắt buộc nhập ảnh", null);
                var imageList = areaTypeCateFromDb.Images;
                foreach (var image in images)
                {
                    var item = imageList.FirstOrDefault(x => x.ImageUrl == $"{image}");
                    if (item == null)
                        return new ResponseDTO<AreaTypeCategory>(400, "Ảnh không tồn tại trong loại khu vực!", null);
                    areaTypeCateFromDb.Images.Remove(item);

                }
                await UpdateImage(areaTypeCateFromDb, images);
                foreach (var image in images)
                {
                    ImageSerive.RemoveImage(image);
                }
                
                return new ResponseDTO<AreaTypeCategory>(200, "Cập nhập thành công!", null);

            }
            catch (Exception ex)
            {
                return new ResponseDTO<AreaTypeCategory>(400, "Cập nhập lỗi!", null);
            }
        }
    }
}

