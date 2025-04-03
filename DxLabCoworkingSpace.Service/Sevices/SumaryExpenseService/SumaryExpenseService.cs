using DxLabCoworkingSpac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class SumaryExpenseService : ISumaryExpenseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SumaryExpenseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(SumaryExpense entity)
        {
            await _unitOfWork.SumaryExpenseRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task Add(List<SumaryExpense> sumaryExpenses)
        {
            foreach(var item in sumaryExpenses)
            {
               await _unitOfWork.SumaryExpenseRepository.Add(item);
            }
            await _unitOfWork.CommitAsync();

        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Task<SumaryExpense> Get(Expression<Func<SumaryExpense, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<SumaryExpense>> GetAll()
        {
            return await _unitOfWork.SumaryExpenseRepository.GetAll();
        }

        public async Task<IEnumerable<SumaryExpense>> GetAll(Expression<Func<SumaryExpense, bool>> expression)
        {
            return await _unitOfWork.SumaryExpenseRepository.GetAll(expression);
        }

        public Task<IEnumerable<SumaryExpense>> GetAllWithInclude(params Expression<Func<SumaryExpense, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task<SumaryExpense> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<SumaryExpense> GetWithInclude(Expression<Func<SumaryExpense, bool>> expression, params Expression<Func<SumaryExpense, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task Update(SumaryExpense entity)
        {
            throw new NotImplementedException();
        }
    }
}
