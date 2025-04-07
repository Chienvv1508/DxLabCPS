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
            return await _unitOfWork.AreaTypeRepository.GetAllWithInclude( x => x.Images);
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
            var listAreaType = await _unitOfWork.AreaTypeRepository.GetAll();
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

        public async Task UpdateImage(int id, AreaType areaTypeFromDb, List<IFormFile> newImages)
        {
            var areaType = await GetWithInclude(b => b.AreaTypeId == id, x => x.Images);
            if (areaType == null)
            {
                throw new Exception("Không tìm thấy AreaType");
            }

            // Xóa ảnh cũ
            if (areaType.Images != null && areaType.Images.Any())
            {
                foreach (var image in areaType.Images.ToList())
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    _unitOfWork.Context.Set<Image>().Remove(image);
                }
                areaType.Images.Clear();
            }

            // Thêm ảnh mới từ newImages
            if (newImages != null && newImages.Any())
            {
                var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                Directory.CreateDirectory(imagesDir);
                areaTypeFromDb.Images.Clear();

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

                        areaTypeFromDb.Images.Add(new Image { ImageUrl = $"/images/{uniqueFileName}" });
                    }
                }
            }

            // Lưu thay đổi
            await _unitOfWork.AreaTypeRepository.Update(areaTypeFromDb);
            await _unitOfWork.CommitAsync();
        }
    }
}
