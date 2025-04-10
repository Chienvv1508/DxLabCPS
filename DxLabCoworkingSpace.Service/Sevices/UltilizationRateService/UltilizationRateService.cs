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
        public Task Add(UltilizationRate entity)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Task<UltilizationRate> Get(Expression<Func<UltilizationRate, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UltilizationRate>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UltilizationRate>> GetAll(Expression<Func<UltilizationRate, bool>> expression)
        {
            throw new NotImplementedException();
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
