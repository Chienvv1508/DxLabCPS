using DxLabCoworkingSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{

    public class DepreciationService : IDepreciationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DepreciationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task Add(DepreciationSum entity)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<DepreciationSum> Get(Expression<Func<DepreciationSum, bool>> expression)
        {
           return await _unitOfWork.DepreciationSumRepository.Get(expression);
        }

        public Task<IEnumerable<DepreciationSum>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DepreciationSum>> GetAll(Expression<Func<DepreciationSum, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<DepreciationSum>> GetAllWithInclude(params Expression<Func<DepreciationSum, object>>[] includes)
        {
          return await _unitOfWork.DepreciationSumRepository.GetAllWithInclude(includes);
           

        }

        public async Task<IEnumerable<DepreciationSum>> GetAllWithInclude(Expression<Func<DepreciationSum, bool>> expression, params Expression<Func<DepreciationSum, object>>[] includes)
        {
            var x = await _unitOfWork.DepreciationSumRepository.GetAllWithInclude(includes);
            return x.AsQueryable().Where(expression);
        }

        public Task<DepreciationSum> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<DepreciationSum> GetWithInclude(Expression<Func<DepreciationSum, bool>> expression, params Expression<Func<DepreciationSum, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task Update(DepreciationSum entity)
        {
            throw new NotImplementedException();
        }
    }
}
