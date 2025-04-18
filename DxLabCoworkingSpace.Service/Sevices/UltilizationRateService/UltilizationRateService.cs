using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class UltilizationRateService : IUltilizationRateService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UltilizationRateService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(UltilizationRate entity)
        {
            try
            {
              await  _unitOfWork.UltilizationRateRepository.Add(entity);
                await _unitOfWork.CommitAsync();
            }
            catch(Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }
        }

        public Task Add(IUltilizationRateService ultilizationRateService)
        {
            throw new NotImplementedException();
        }

        public async Task Add(List<UltilizationRate> ultilizationRates)
        {
            if (ultilizationRates == null) return;
            try
            {
                foreach(var i in ultilizationRates)
                {
                    await _unitOfWork.UltilizationRateRepository.Add(i);
                }
                
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<UltilizationRate> Get(Expression<Func<UltilizationRate, bool>> expression)
        {
            return await _unitOfWork.UltilizationRateRepository.Get(expression);
        }

        public Task<IEnumerable<UltilizationRate>> GetAll()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<UltilizationRate>> GetAll(Expression<Func<UltilizationRate, bool>> expression)
        {
            return await _unitOfWork.UltilizationRateRepository.GetAll(expression);
        }

        public Task<IEnumerable<UltilizationRate>> GetAllWithInclude(params Expression<Func<UltilizationRate, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task<UltilizationRate> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<UltilizationRate> GetWithInclude(Expression<Func<UltilizationRate, bool>> expression, params Expression<Func<UltilizationRate, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task Update(UltilizationRate entity)
        {
            throw new NotImplementedException();
        }
    }
}
