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
    }
}
