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

        public Task<IEnumerable<AreaTypeCategory>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<AreaTypeCategory>> GetAll(Expression<Func<AreaTypeCategory, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<AreaTypeCategory>> GetAllWithInclude(params Expression<Func<AreaTypeCategory, object>>[] includes)
        {
            return await _unitOfWork.AreaTypeCategoryRepository.GetAllWithInclude(includes);
        }

        public Task<AreaTypeCategory> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<AreaTypeCategory> GetWithInclude(Expression<Func<AreaTypeCategory, bool>> expression, params Expression<Func<AreaTypeCategory, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public async Task Update(AreaTypeCategory entity)
        {
            await _unitOfWork.AreaTypeCategoryRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task updateAreaTypeCategory(int id, AreaTypeCategory updatedAreaTypeCategory)
        {
            var AreaTypeCategory = await GetWithInclude(b => b.CategoryId == id, x => x.Images);
            Console.WriteLine(1);
            if (AreaTypeCategory == null)
            {
                throw new Exception("Không tìm thấy AreaType");
            }



            // Xóa các file ảnh vật lý trong wwwroot/images (nếu có)
            if (AreaTypeCategory.Images != null && AreaTypeCategory.Images.Any())
            {
                foreach (var image in AreaTypeCategory.Images)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                // Xóa các bản ghi ảnh trong cơ sở dữ liệu
                _unitOfWork.Context.Set<Image>().RemoveRange(AreaTypeCategory.Images);
                AreaTypeCategory.Images.Clear(); // Làm sạch danh sách ảnh trong bộ nhớ
            }
            Console.WriteLine(2);

            // Cập nhật thông tin blog
            AreaTypeCategory.Title = updatedAreaTypeCategory.Title;
            AreaTypeCategory.CategoryDescription = updatedAreaTypeCategory.CategoryDescription;
            // Thêm ảnh mới (nếu có)
            if (AreaTypeCategory.Images != null)
            {
                AreaTypeCategory.Images = updatedAreaTypeCategory.Images.Select(img => new Image { ImageUrl = img.ImageUrl }).ToList();
            }
            Console.WriteLine(3);
            await _unitOfWork.AreaTypeCategoryRepository.Update(AreaTypeCategory);
            Console.WriteLine(4);
            await _unitOfWork.CommitAsync();
            Console.WriteLine(5);
        }
    }
}

