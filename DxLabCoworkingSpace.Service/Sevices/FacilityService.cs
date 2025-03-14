using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Service.Sevices
{
    public class FacilityService : IFacilityService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FacilityService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task AddFacilityFromExcel(List<Facility> facilities)
        {

            throw new NotImplementedException();
        }

        public async Task Add(Facility entity)
        {
            var existingFacility = await _unitOfWork.FacilityRepository.Get(f => f.BatchNumber == entity.BatchNumber);
            if (existingFacility != null)
            {
                throw new InvalidOperationException("BatchNumber đã tồn tại!");
            }

            await _unitOfWork.FacilityRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task<Facility> Get(Expression<Func<Facility, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Facility>> GetAll()
        {
            return await _unitOfWork.FacilityRepository.GetAll();
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
