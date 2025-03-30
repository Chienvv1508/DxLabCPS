using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{


    public class UsingFacilityService : IUsingFacilytyService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UsingFacilityService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(UsingFacility entity)
        {
          await _unitOfWork.UsingFacilityRepository.Add(entity);
            await _unitOfWork.CommitAsync();  
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<UsingFacility> Get(Expression<Func<UsingFacility, bool>> expression)
        {
            return await _unitOfWork.UsingFacilityRepository.Get(expression);
        }

        public async Task<IEnumerable<UsingFacility>> GetAll()
        {
            return await _unitOfWork.UsingFacilityRepository.GetAll();
        }

        public async Task<IEnumerable<UsingFacility>> GetAll(Expression<Func<UsingFacility, bool>> expression)
        {
            return await _unitOfWork.UsingFacilityRepository.GetAll(expression);
        }

        public async Task<IEnumerable<UsingFacility>> GetAllWithInclude(params Expression<Func<UsingFacility, object>>[] includes)
        {
            return await _unitOfWork.UsingFacilityRepository.GetAllWithInclude(includes);
        }

        public async Task<IEnumerable<UsingFacility>> GetAllWithInclude(Expression<Func<UsingFacility, bool>> expression, params Expression<Func<UsingFacility, object>>[] includes)
        {
            IQueryable<UsingFacility> x = (IQueryable < UsingFacility >) await _unitOfWork.UsingFacilityRepository.GetAllWithInclude(includes);
            return x.Where(expression).ToList();;
        }

        public Task<UsingFacility> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<UsingFacility> GetWithInclude(Expression<Func<UsingFacility, bool>> expression, params Expression<Func<UsingFacility, object>>[] includes)
        {
            return await _unitOfWork.UsingFacilityRepository.GetWithInclude(expression, includes);
        }

        public async Task Update(UsingFacility entity)
        {
            await _unitOfWork.UsingFacilityRepository.Update(entity);
        }
    }
}
