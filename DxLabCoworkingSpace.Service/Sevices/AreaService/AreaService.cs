using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AreaService : IAreaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AreaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task Add(Area entity)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<Area> Get(Expression<Func<Area, bool>> expression)
        {
            return await _unitOfWork.AreaRepository.Get(expression);
        }

        public async Task<IEnumerable<Area>> GetAll()
        {
            return await _unitOfWork.AreaRepository.GetAll();
        }

        public async Task<IEnumerable<Area>> GetAll(Expression<Func<Area, bool>> expression)
        {
            return await _unitOfWork.AreaRepository.GetAll(expression);
        }

        public Task<Area> GetById(int id)
        {
            throw new NotImplementedException();
        }
        public async Task<IEnumerable<Area>> GetAllWithInclude(params Expression<Func<Area, object>>[] includes)
        {
            throw new NotImplementedException();
        }
        public async Task<Area> GetWithInclude(Expression<Func<Area, bool>> expression, params Expression<Func<Area, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task Update(Area entity)
        {
            throw new NotImplementedException();
        }
    }
}
