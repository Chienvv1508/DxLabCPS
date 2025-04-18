using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

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

            return await _unitOfWork.AreaTypeCategoryRepository.GetWithInclude(expression,includes);

        }

        public async Task Update(AreaTypeCategory entity)
        {
            await _unitOfWork.AreaTypeCategoryRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task updateAreaTypeCategory(int id, AreaTypeCategory updatedAreaTypeCategory, List<IFormFile> newImages)
        {
            var areaTypeCategory = await GetWithInclude(b => b.CategoryId == id, x => x.Images);
            if (areaTypeCategory == null)
            {
                throw new Exception("Không tìm thấy AreaType");
            }

            // Xóa ảnh cũ
            if (areaTypeCategory.Images != null && areaTypeCategory.Images.Any())
            {
                foreach (var image in areaTypeCategory.Images.ToList())
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    _unitOfWork.Context.Set<Image>().Remove(image);
                }
                areaTypeCategory.Images.Clear();
            }

            // Thêm ảnh mới từ newImages
            if (newImages != null && newImages.Any())
            {
                var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                Directory.CreateDirectory(imagesDir);

                foreach (var file in newImages)
                {
                    if (file.Length > 0)
                    {
                        if (!file.FileName.EndsWith(".jpg") && !file.FileName.EndsWith(".png"))
                            throw new Exception("Chỉ chấp nhận file .jpg hoặc .png!");
                        if (file.Length > 5 * 1024 * 1024)
                            throw new Exception("File quá lớn, tối đa 5MB!");

                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(file.FileName)}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(imagesDir, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        areaTypeCategory.Images.Add(new Image { ImageUrl = $"/images/{uniqueFileName}" });
                    }
                }
            }

            // Cập nhật thông tin khác
            areaTypeCategory.Title = updatedAreaTypeCategory.Title;
            areaTypeCategory.CategoryDescription = updatedAreaTypeCategory.CategoryDescription;

            // Lưu thay đổi
            await _unitOfWork.AreaTypeCategoryRepository.Update(areaTypeCategory);
            await _unitOfWork.CommitAsync();
        }

        public async Task UpdateImage(AreaTypeCategory areaTypeCateFromDb, List<string> images)
        {
            try
            {
                var listImage = await _unitOfWork.ImageRepository.GetAll(x => x.AreaTypeCategoryId == areaTypeCateFromDb.CategoryId);
                if (images == null)
                    throw new ArgumentNullException();
                foreach(var item in images)
                {
                    var x = listImage.FirstOrDefault(x => x.ImageUrl == item);
                    if (x == null) throw new Exception("Ảnh nhập vào không phù hợp");
                   await _unitOfWork.ImageRepository.Delete(x.ImageId);

                }
                await _unitOfWork.AreaTypeCategoryRepository.Update(areaTypeCateFromDb);
                await _unitOfWork.CommitAsync();
            }
            catch(Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }
        }
    }
}

