using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Service.Sevices
{
    public class FacilitiesService : IFacilitiesService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FacilitiesService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(Facility entity)
        {
            await _unitOfWork.FacilityRepository.Add(entity);
            _unitOfWork.CommitAsync();
        }

        public async Task<Facility> Get(Expression<Func<Facility, bool>> expression)
        {
            return await _unitOfWork.FacilityRepository.Get(expression);
        }

        public async Task<IEnumerable<Facility>> GetAll()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Facility>> GetAll(Expression<Func<Facility, bool>> expression)
        {
            throw new NotImplementedException();
        }
        async Task<Facility> IGenericService<Facility>.GetById(int id)
        {
            return await _unitOfWork.FacilityRepository.GetById(id);
        }

        public async Task Update(Facility entity)
        {
            throw new NotImplementedException();
        }
        async Task IGenericService<Facility>.Delete(int id)
        {
            throw new NotImplementedException();
        }

    }
}
