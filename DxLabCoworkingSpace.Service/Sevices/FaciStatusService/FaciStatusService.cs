using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class FaciStatusService : IFaciStatusService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FaciStatusService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(FacilitiesStatus entity)
        {
            await _unitOfWork.FacilitiesStatusRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<FacilitiesStatus> Get(Expression<Func<FacilitiesStatus, bool>> expression)
        {
          return  await _unitOfWork.FacilitiesStatusRepository.Get(expression);
        }

        public async Task<IEnumerable<FacilitiesStatus>> GetAll()
        {
            return await _unitOfWork.FacilitiesStatusRepository.GetAll();
        }

        public async Task<IEnumerable<FacilitiesStatus>> GetAll(Expression<Func<FacilitiesStatus, bool>> expression)
        {
            return await _unitOfWork.FacilitiesStatusRepository.GetAll(expression);
        }

        public async Task<IEnumerable<FacilitiesStatus>> GetAllWithInclude(params Expression<Func<FacilitiesStatus, object>>[] includes)
        {
            return await _unitOfWork.FacilitiesStatusRepository.GetAllWithInclude(includes);
        }

        public async Task<IEnumerable<FacilitiesStatus>> GetAllWithInclude(Expression<Func<FacilitiesStatus, bool>> expression, params Expression<Func<FacilitiesStatus, object>>[] includes)
        {
            var data = await _unitOfWork.FacilitiesStatusRepository.GetAllWithInclude(includes);

            var query = data.AsQueryable().Where(expression);

            return query.ToList();
        }

        public async Task<FacilitiesStatus> GetById(int id)
        {
            return await _unitOfWork.FacilitiesStatusRepository.GetById(id);
        }

        public async Task<FacilitiesStatus> GetWithInclude(Expression<Func<FacilitiesStatus, bool>> expression, params Expression<Func<FacilitiesStatus, object>>[] includes)
        {
            return await _unitOfWork.FacilitiesStatusRepository.GetWithInclude(expression, includes);
        }

        public async Task Update(FacilitiesStatus entity)
        {
             await _unitOfWork.FacilitiesStatusRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }
    }
}
