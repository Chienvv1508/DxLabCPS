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
